# StockLens: Intelligent Inventory Dashboard

Real-time dealership inventory management: surface aging stock, move vehicles through their
lifecycle with an audit trail, log actions on them, manage pricing and disposition strategies at
factory, vehicle-type, or single-vehicle scope, and track sales-team performance.

## Projects

| Folder | Stack | Purpose |
| --- | --- | --- |
| `StockLens.Api` | .NET 10 minimal API, clean architecture, EF Core + PostgreSQL, SignalR, Serilog | REST API and real-time hub |
| `StockLens.App` | Angular 20, standalone components, signals, `@microsoft/signalr` | Dashboard web app |

### Backend layout (clean architecture)

```
StockLens.Api/
├─ src/
│  ├─ StockLens.Domain          # Entities, enums, aging/inventory policy (no dependencies)
│  ├─ StockLens.Application     # DTOs, interfaces, strategy resolver, pricing/status/dashboard services, validators
│  ├─ StockLens.Infrastructure  # EF Core DbContext, entity configs, migrations, seeder
│  └─ StockLens.Api             # Minimal-API endpoints, SignalR hub, request-logging middleware, DI, Program.cs
├─ tests/
│  ├─ StockLens.Domain.Tests    # Aging and strategy-resolution unit tests
│  └─ StockLens.Api.Tests       # WebApplicationFactory integration tests (inventory and status workflows)
└─ docker-compose.yml           # PostgreSQL 16
```

## Requirements covered

1. **Inventory visualization**: filterable list (make, model, status, age, free-text search,
   sort, paging) with a per-vehicle detail view.
2. **Aging stock identification**: vehicles in stock past the aging threshold (90 days) are
   flagged (`isAgingStock`), highlighted in the table, counted on the dashboard, and available
   at `GET /api/vehicles/aging`.
3. **Vehicle lifecycle**: vehicles move through the Open, Deposited, Hold, and Sold states via a
   dedicated status endpoint that validates the transition, captures the required evidence
   (deposit amount, salesperson, sold date), and records every change as an audit trail
   (`GET /api/vehicles/{id}/status-history`). Every state except Sold still ages.
4. **Actionable insights**: managers log and persist actions per vehicle
   (for example *Price Reduction Planned*) with status tracking; actions on sold vehicles are blocked.
5. **Strategy-driven pricing**: the effective business-strategy discount is resolved on read
   (never stored), so each vehicle reports a live `discountPercent` and `netPrice` that stay
   correct as strategies are added, edited, or expire.
6. **Business strategies**: create and update at factory (make), vehicle-type (make + model),
   or specific-vehicle scope; the API resolves the most specific in-effect match per vehicle.
7. **Sales performance**: sold vehicles are attributed to a salesperson; the dashboard shows
   top sales and a monthly sales trend, and `GET /api/salespeople` reports each member's
   lifetime unit count and revenue.
8. **Dashboard KPIs**: total in stock, aging count, stock value, average days-in-inventory,
   average days-to-sell, 30-day sold count and revenue, stock-by-make breakdown, and the sales trend.
9. **Real time**: every mutation broadcasts over SignalR (`/hubs/inventory`); the dashboard,
   inventory list, and toast notifications update live.
10. **Observability**: Serilog structured logging with a per-request correlation id
    (`X-Correlation-Id`) attached to every log line via request-logging middleware.

## Prerequisites

- .NET 10 SDK
- Node 20+ and Angular CLI 20 (`npm i -g @angular/cli`)
- Docker Desktop

## Run it in VS Code (recommended)

Open the **repository root** (`StockLens/`), not a subfolder, so `.vscode/` is picked up.
Install the recommended extensions when prompted (C# Dev Kit, Angular Language Service, Docker).

Press **F5** and pick a configuration:

| Configuration | What it does |
| --- | --- |
| **StockLens.API** | Starts the Postgres container, builds the solution, debugs the API on `http://localhost:5080`, and opens Swagger |
| **StockLens.App (Chrome)** | Runs the Angular dev server and opens `http://localhost:4200` |
| **StockLens (API + App)** | Compound; runs both together |

The API's `preLaunchTask` (`prepare-api`) runs `start-database` then `build-api`, so the
database is up before the API applies EF migrations on startup. Other tasks are available via
**Terminal → Run Task**: `start-database`, `stop-database`, `build-api`, `test-api`, `start-app`.

> The API port is **5080** in both `.vscode/launch.json` and `Properties/launchSettings.json`,
> matching `API_BASE` in `StockLens.App/src/app/core/config.ts` and the CORS origin. If you
> change it, change it in all three places.

## Run it from the CLI

### 1. Database

```bash
cd StockLens.Api
docker compose up -d          # PostgreSQL on host port 5433 (mapped to avoid a local 5432 clash)
```

### 2. API

```bash
cd StockLens.Api/src/StockLens.Api
dotnet build                  # compile only
dotnet run                    # build and run
```

Listens on `http://localhost:5080` (Swagger UI at `/swagger`; `/` redirects there). On startup
the API applies EF migrations and seeds representative data (in-stock, deposited, held, and
aging vehicles; salespeople; sales history; strategies at every scope).

> The connection string lives in `appsettings.json` and `appsettings.Development.json`
> (`Host=localhost;Port=5433;Database=stocklens;Username=stocklens;Password=stocklens`).
> Change the port back to `5432` (here and in `docker-compose.yml`) if you have no local
> Postgres occupying it.

### 3. Web app

```bash
cd StockLens.App
npm install
npm run build                 # production build to dist/
npm start                     # dev server at http://localhost:4200
```

If the API is not on `http://localhost:5080`, update `API_BASE` in
`StockLens.App/src/app/core/config.ts`.

## Tests

The suite validates the core business logic of the dashboard: inventory aging, strategy-driven
pricing, and the vehicle lifecycle workflow.

```bash
cd StockLens.Api
dotnet test                   # domain unit tests + API integration tests (needs the DB up)
```

- **Domain unit tests** (`StockLens.Domain.Tests`): the aging-stock threshold and
  days-in-inventory math (including sold and held vehicles), and strategy-resolution precedence
  (Vehicle over VehicleType over Factory, ignoring inactive or expired strategies, newest wins at
  the same scope).
- **API integration tests** (`StockLens.Api.Tests`), run against a real host via
  `WebApplicationFactory`:
  - **Inventory**: dashboard KPIs and the sales trend, salesperson attribution on top sales, the
    effective-strategy discount applied to list price, the aging endpoint, action logging,
    multi-term search across fields, and request validation.
  - **Vehicle lifecycle**: each status transition and the evidence it requires (deposit amount and
    salesperson, hold reason, sale date/salesperson/price), selling creating a sale that reaches
    the dashboard, un-selling reversing it so revenue is not left inflated, the status-history
    audit trail, and rejected moves (same status, unknown salesperson, unknown vehicle).

## Key endpoints

| Method | Route | Notes |
| --- | --- | --- |
| GET | `/api/vehicles` | Filter: `make, model, status, agingOnly, minAgeDays, maxAgeDays, search, sortBy, desc, page, pageSize` |
| GET | `/api/vehicles/aging` | Aging stock (past the 90-day threshold) |
| GET | `/api/vehicles/{id}` | Single vehicle (with resolved `netPrice`) |
| POST/PUT | `/api/vehicles`, `/api/vehicles/{id}` | Add or update a vehicle |
| POST | `/api/vehicles/{id}/status` | Change lifecycle status with required evidence |
| GET | `/api/vehicles/{id}/status-history` | Audit trail of status changes |
| GET/POST | `/api/vehicles/{id}/actions` | List or log actions |
| PUT | `/api/actions/{id}` | Update an action's status |
| GET | `/api/vehicles/{id}/effective-strategy` | Resolved strategy (Vehicle over VehicleType over Factory) |
| GET/POST/PUT/DELETE | `/api/strategies` | Manage strategies at any scope |
| GET | `/api/strategies/scope-options` | Available makes, vehicle types, and vehicles for scoping |
| GET | `/api/salespeople` | Sales team with lifetime unit count and revenue (`activeOnly` filter) |
| GET | `/api/dashboard/summary` | KPIs, top sales, stock-by-make, monthly sales trend |
| GET | `/health` | Full health report (JSON) with a per-check breakdown |
| GET | `/health/ready` | Readiness probe: database reachability (checks tagged `ready`) |
| GET | `/health/live` | Liveness probe: process is up (no dependency checks) |
| Hub | `/hubs/inventory` | SignalR: `VehicleChanged`, `ActionChanged`, `StrategyChanged`, `DashboardChanged` |

## AI Collaboration Narrative

StockLens was built with generative AI (Claude, via Claude Code) as a pair programmer, with a
human owning the architecture and holding the final say on every change.

### Guiding the AI

- **Standards as durable context.** The engineering standards live in a project `CLAUDE.md` and a
  set of skills (project-standards, dotnet-clean-api, angular-enterprise, feature-generator,
  inventory-domain-expert, code-reviewer). The AI reads these on every task, so generated code
  starts inside the guardrails (Clean Architecture, minimal APIs, typed results, FluentValidation,
  Angular signals with OnPush) instead of being corrected into them afterward.
- **Feature-sized prompts.** Work was requested one vertical slice at a time (for example the
  vehicle status workflow, or strategy-driven pricing), with the domain rules stated up front
  (aging at 90 days, strategy precedence Vehicle over VehicleType over Factory). Small units kept
  each change reviewable.
- **Explicit decisions when the defaults did not fit.** Where the AI's assumptions clashed with the
  project's intent, the human decided and the decision was recorded: PostgreSQL was kept over SQL
  Server, a custom SCSS UI over Angular Material, and Serilog-only logging was chosen for now. The
  AI accelerated the work; it did not make the architectural calls.

### Verifying and refining the output

- **Build and run, not just read.** Every change was compiled and the app was run end to end, with
  the live dashboard and SignalR updates exercised in the browser before a feature was considered
  done.
- **Tests as the safety net.** Core business logic is covered by domain unit tests and
  `WebApplicationFactory` integration tests (see [Tests](#tests)). New behavior was accompanied by
  tests, and the suite was kept green as a gate on each change.
- **Review passes.** Generated code was reviewed against the standards, both by hand and with the
  code-reviewer skill, which drove uniform fixes such as OnPush across components and `AsNoTracking`
  across read-only queries.

### Ensuring final quality

- **Correctness:** derived values (aging, discount, net price) are computed on read, so there is no
  denormalized state to drift, and the tests pin the rules that matter.
- **Security and validation:** all client input is validated, EF Core parameterizes queries, search
  wildcards are escaped, CORS is restricted to the app origin, and no secrets or sensitive data are
  logged.
- **Consistency and readability:** naming, structure, and prose were held to the house conventions,
  favoring readable code over clever code.
- **Human judgment last.** The AI produced drafts and options at speed; a human verified behavior,
  checked the tests, and approved every change before it landed.

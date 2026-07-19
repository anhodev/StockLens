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

## Step-by-step setup on a new machine

Follow these steps in order on a machine that has none of the tooling installed. Each step ends with
a command that confirms it worked before you move on. Commands are shown for Windows (`winget`) and
macOS (`brew`); on either OS you can also use the installer links instead. After installing a tool,
open a **new terminal** so the updated `PATH` is picked up.

By the end the app runs at `http://localhost:4200` and the API at `http://localhost:5080`.

### Step 1: Install Node.js

Node runs the Angular web app and its build.

- Windows: `winget install OpenJS.NodeJS.LTS`
- macOS: `brew install node`
- Or download the **LTS** installer from [nodejs.org](https://nodejs.org/).

Verify (need Node 20 or newer):

```bash
node --version
npm --version
```

### Step 2: Install Git

Git is used to download (clone) the source code.

- Windows: `winget install Git.Git`
- macOS: `brew install git` (or run `git` once to trigger Xcode command line tools)
- Or download from [git-scm.com](https://git-scm.com/downloads).

Verify:

```bash
git --version
```

### Step 3: Clone the code

Pick a folder for your projects, then clone the repository into it and move into the repo root.

```bash
git clone <repository-url> StockLens
cd StockLens
```

`<repository-url>` is the HTTPS or SSH URL from your Git host. Everything below is run from this
`StockLens/` root unless a step says otherwise.

### Step 4: Run the Angular app

Install the web app's dependencies and start its dev server.

```bash
cd StockLens.App
npm install                   # first run only; downloads dependencies, takes a few minutes
npm start                     # starts the dev server
```

Open `http://localhost:4200`. The app loads and reloads on save. It will show **no data yet**
because the API is not running; that is expected and is fixed in steps 5 to 7. Leave this terminal
running and open a new one for the next steps.

### Step 5: Install Docker Desktop

The database (PostgreSQL) runs inside a Docker container, so you do not have to install PostgreSQL
directly.

- Download and install **Docker Desktop** from
  [docker.com/products/docker-desktop](https://www.docker.com/products/docker-desktop/).
- **Start Docker Desktop and wait until it says it is running** (the whale icon stops animating).
  Nothing in the next step works until the Docker engine is actually up.

Verify:

```bash
docker --version
docker info                   # succeeds only when Docker Desktop is running
```

### Step 6: Create the PostgreSQL database with Docker Compose

From the repo root, start the database container. The first run downloads the PostgreSQL image, so
give it a moment.

```bash
cd StockLens.Api
docker compose up -d          # creates and starts the "stocklens-db" container on port 5433
docker compose ps             # wait until STATUS shows "healthy"
```

The database keeps its data in a Docker volume, so it survives restarts. Stop it later with
`docker compose down` (add `-v` to also erase the data and start fresh).

### Step 7: Run the API

With the database healthy, start the API from its project folder.

```bash
cd StockLens.Api/src/StockLens.Api
dotnet run                    # restores, builds, and runs the API
```

> Requires the **.NET 10 SDK**. If `dotnet --version` reports less than 10 or the command is not
> found, install it first: Windows `winget install Microsoft.DotNet.SDK.10`, macOS
> `brew install --cask dotnet-sdk`, or from
> [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download/dotnet/10.0).

On the first start the API creates the database schema (EF Core migrations) and seeds sample data,
so it takes a little longer than later runs. It listens on `http://localhost:5080`. Confirm it is
up:

```bash
curl http://localhost:5080/health/ready     # database reachable
```

Swagger UI is at `http://localhost:5080/swagger`.

### Step 8: See it working

Go back to the browser tab at `http://localhost:4200` and refresh. The dashboard now shows live
inventory data served by the API, with real-time updates over SignalR.

---

The sections below are reference material: a per-tool prerequisites table, the same run steps as VS
Code launch configs, tests, endpoints, and troubleshooting.

## Prerequisites (reference)

The step-by-step guide above installs each of these in order. This table is the at-a-glance summary:
the version to have, how to install it, and the command that confirms it is on your `PATH`.

| Tool | Minimum version | Install | Verify |
| --- | --- | --- | --- |
| .NET SDK | 10.0 | [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download/dotnet/10.0) (or `winget install Microsoft.DotNet.SDK.10` / `brew install --cask dotnet-sdk`) | `dotnet --version` |
| Node.js + npm | Node 20 LTS or newer | [nodejs.org](https://nodejs.org/) (or `winget install OpenJS.NodeJS.LTS` / `brew install node`) | `node --version` and `npm --version` |
| Angular CLI | 20 | `npm install -g @angular/cli@20` | `ng version` |
| Docker Desktop | current | [docker.com/products/docker-desktop](https://www.docker.com/products/docker-desktop/) | `docker --version` (start Docker Desktop so the engine is running) |
| Git | current | [git-scm.com](https://git-scm.com/downloads) | `git --version` |

The Angular CLI is optional if you only use the `npm` scripts, but the VS Code launch config and the
`ng` commands in this document assume it is installed globally.

## Run it in VS Code (recommended)

The repository ships its own VS Code workspace under `.vscode/` (`launch.json`, `tasks.json`,
`extensions.json`), so debug configs and build tasks are already wired up. You only need to do the
one-time setup below once per machine.

### First-time VS Code setup

1. **Install VS Code** from [code.visualstudio.com](https://code.visualstudio.com/), plus the
   [prerequisites](#prerequisites-reference) (.NET 10 SDK, Node, Docker Desktop). Start Docker Desktop.
2. **Open the repository root** (`StockLens/`), not a subfolder: `File → Open Folder…` and pick the
   `StockLens/` directory. The `.vscode/` configs are only picked up when the root is the workspace.
3. **Install the recommended extensions.** VS Code prompts "This workspace has extension
   recommendations" the first time; click **Install All**. They come from `.vscode/extensions.json`:

   | Extension | Why |
   | --- | --- |
   | C# Dev Kit (`ms-dotnettools.csdevkit`) | Build and debug the .NET API |
   | C# (`ms-dotnettools.csharp`) | C# language support (installed with Dev Kit) |
   | Angular Language Service (`angular.ng-template`) | Angular template IntelliSense |
   | Docker (`ms-azuretools.vscode-docker`) | Manage the Postgres container |

   You can also open the Extensions panel (`Ctrl+Shift+X`), type `@recommended`, and install from there.
4. **Install the web app dependencies once.** The app launch runs `npm start`, which needs
   `node_modules`, but no task installs them for you. Open a terminal (`Ctrl+` `` ` ``) and run:

   ```bash
   cd StockLens.App
   npm install
   ```

   Skipping this makes the **StockLens.App** launch fail with "ng: command not found" or missing
   modules. The API side needs no equivalent step: its `build-api` task restores NuGet packages.

### Run and debug

Press **F5** (or open the **Run and Debug** panel, `Ctrl+Shift+D`) and pick a configuration from the
dropdown:

| Configuration | What it does |
| --- | --- |
| **StockLens.API** | Starts the Postgres container, builds the solution, debugs the API on `http://localhost:5080`, and opens Swagger. Breakpoints in C# work. |
| **StockLens.App (Chrome)** | Runs the Angular dev server and launches Chrome at `http://localhost:4200`. Breakpoints in TypeScript work. |
| **StockLens (API + App)** | Compound; runs both of the above together. Use this for the full stack. Stopping one stops both. |

Behind the scenes the API's `preLaunchTask` (`prepare-api`) runs `start-database` then `build-api`
in sequence, so the database is up before the API applies EF migrations on startup, and the app's
`preLaunchTask` (`start-app`) boots the dev server. First launch is slower while the DB image pulls
and the app compiles.

### Tasks you can run on their own

Open the Command Palette (`Ctrl+Shift+P`) → **Tasks: Run Task**, or **Terminal → Run Task**:

| Task | Does |
| --- | --- |
| `start-database` / `stop-database` | Start or stop the Postgres container |
| `build-api` | Build the .NET solution (also the default build task, `Ctrl+Shift+B`) |
| `test-api` | Run the backend unit + integration tests |
| `start-app` | Start the Angular dev server on `http://localhost:4200` |

> The API port is **5080** in both `.vscode/launch.json` and `Properties/launchSettings.json`,
> matching `API_BASE` in `StockLens.App/src/app/core/config.ts` and the CORS origin. If you
> change it, change it in all three places.

## Run it from the CLI

Run the three steps in order, each in its own terminal (the API and the web app are long-running).
All paths below are relative to the repository root.

### 1. Database

Make sure Docker Desktop is running, then start PostgreSQL:

```bash
cd StockLens.Api
docker compose up -d          # PostgreSQL 16 on host port 5433 (mapped to avoid a local 5432 clash)
docker compose ps             # confirm the "stocklens-db" container is "healthy"
```

The container has a health check, so give it a few seconds to report `healthy` before starting the
API. Data persists in the `stocklens_pgdata` Docker volume across restarts. To stop it later use
`docker compose down` (add `-v` to also delete the volume and start from an empty database).

### 2. API

```bash
cd StockLens.Api/src/StockLens.Api
dotnet restore                # restore NuGet packages (first run only)
dotnet build                  # compile only
dotnet run                    # build and run
```

Listens on `http://localhost:5080` (Swagger UI at `/swagger`; `/` redirects there). On the first
startup the API applies EF migrations and seeds representative data (in-stock, deposited, held, and
aging vehicles; salespeople; sales history; strategies at every scope). Confirm it is up with a
quick health check:

```bash
curl http://localhost:5080/health/live     # -> process is up
curl http://localhost:5080/health/ready     # -> database is reachable
```

> The connection string lives in `appsettings.json` and `appsettings.Development.json`
> (`Host=localhost;Port=5433;Database=stocklens;Username=stocklens;Password=stocklens`).
> It must match the port published by `docker-compose.yml`. Change the port back to `5432` (in both
> files) if you would rather use a local Postgres and have none occupying `5432`.

### 3. Web app

```bash
cd StockLens.App
npm install                   # install dependencies (first run only; this is the slow step)
npm start                     # dev server at http://localhost:4200, reloads on save
```

Open `http://localhost:4200`. The dev server proxies nothing on its own; the app calls the API
directly at the address in `API_BASE`, so the API from step 2 must be running. For a production
bundle instead, run `npm run build` (output goes to `dist/`).

> If the API is not on `http://localhost:5080`, update `API_BASE` in
> `StockLens.App/src/app/core/config.ts` and restart the dev server.

## Troubleshooting a fresh setup

| Symptom | Likely cause and fix |
| --- | --- |
| `docker compose` cannot connect / "pipe not found" | Docker Desktop is not running. Start it and wait for the whale icon to settle, then retry. |
| API fails to start with a connection or timeout error | The database container is not `healthy` yet, or the port differs. Check `docker compose ps`, and confirm the connection-string port matches `docker-compose.yml`. |
| Port `5433` (or `5080`, `4200`) already in use | Something else holds the port. Stop it, or change the port in `docker-compose.yml` + `appsettings*.json` (DB), `launchSettings.json` + `.vscode/launch.json` + `API_BASE` (API), or the `ng serve --port` flag (app). |
| Dashboard loads but shows no data / network errors in the browser console | The API is not running, is on a different port, or CORS blocks it. Confirm step 2 is up and that `API_BASE` matches the API origin. |
| `ng: command not found` | The Angular CLI is not installed globally. Run `npm install -g @angular/cli@20`, or use the `npm` scripts (`npm start`, `npm run build`) which do not need it. |
| `dotnet` build errors about the target framework | The installed SDK is older than 10.0. Check `dotnet --version` and install the .NET 10 SDK. |

## Tests

The suite validates the core business logic of the dashboard: inventory aging, strategy-driven
pricing, and the vehicle lifecycle workflow.

```bash
cd StockLens.Api
dotnet test                   # domain unit tests + API integration tests (needs the DB up)
```

Frontend unit tests run through Karma:

```bash
cd StockLens.App
npm test                      # or: ng test
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

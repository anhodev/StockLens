# StockLens — Intelligent Inventory Dashboard

Real-time dealership inventory management: surface aging stock, log actions on it, and
manage pricing/disposition strategies at factory, vehicle-type, or single-vehicle scope.

## Projects

| Folder | Stack | Purpose |
| --- | --- | --- |
| `StockLens.Api` | .NET 10 minimal API, clean architecture, EF Core + PostgreSQL, SignalR | REST API + real-time hub |
| `StockLens.App` | Angular 20, signals, `@microsoft/signalr` | Dashboard web app |

### Backend layout (clean architecture)

```
StockLens.Api/
├─ src/
│  ├─ StockLens.Domain          # Entities, enums, aging rule (no dependencies)
│  ├─ StockLens.Application     # DTOs, interfaces, strategy resolver, validators, dashboard service
│  ├─ StockLens.Infrastructure  # EF Core DbContext, configs, migrations, seeder
│  └─ StockLens.Api             # Minimal-API endpoints, SignalR hub, DI, Program.cs
├─ tests/
│  ├─ StockLens.Domain.Tests    # Aging + strategy-resolution unit tests
│  └─ StockLens.Api.Tests       # WebApplicationFactory integration tests
└─ docker-compose.yml           # PostgreSQL 16
```

## Requirements covered

1. **Inventory visualization** — filterable list (make, model, status, age, search, sort, paging).
2. **Aging stock identification** — vehicles in stock > 90 days are flagged (`isAgingStock`),
   highlighted in the table, counted on the dashboard, and available at `GET /api/vehicles/aging`.
3. **Actionable insights** — managers log/persist actions per vehicle
   (e.g. *Price Reduction Planned*) with status tracking.
4. **Dashboard extras** — top sales, stock value, avg days-in-inventory, avg days-to-sell,
   30-day sold count/revenue, stock-by-make breakdown.
5. **Business strategies** — create/update at factory (make), vehicle-type (make+model),
   or specific-vehicle scope; the API resolves the most specific match per vehicle.
6. **Real time** — every mutation broadcasts over SignalR (`/hubs/inventory`); the dashboard
   and inventory list update live.

## Prerequisites

- .NET 10 SDK
- Node 20+ and Angular CLI 20 (`npm i -g @angular/cli`)
- Docker Desktop

## Run it in VS Code (recommended)

Open the **repository root** (`StockLens/`) — not a subfolder — so `.vscode/` is picked up.
Install the recommended extensions when prompted (C# Dev Kit, Angular Language Service, Docker).

Press **F5** and pick a configuration:

| Configuration | What it does |
| --- | --- |
| **StockLens.API** | Starts the Postgres container, builds the solution, debugs the API on `http://localhost:5080`, and opens Swagger |
| **StockLens.App (Chrome)** | Runs the Angular dev server and opens `http://localhost:4200` |
| **StockLens (API + App)** | Compound — runs both together |

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
dotnet run
```

Listens on `http://localhost:5080` (Swagger UI at `/swagger`). On startup the API applies EF
migrations and seeds representative data (in-stock + aging vehicles, sales, strategies at
every scope).

> The connection string lives in `appsettings.json` / `appsettings.Development.json`
> (`Host=localhost;Port=5433;Database=stocklens;Username=stocklens;Password=stocklens`).
> Change the port back to `5432` (here and in `docker-compose.yml`) if you have no local
> Postgres occupying it.

### 3. Web app

```bash
cd StockLens.App
npm install
npm start                     # http://localhost:4200
```

If the API is not on `http://localhost:5080`, update `API_BASE` in
`StockLens.App/src/app/core/config.ts`.

## Tests

```bash
cd StockLens.Api
dotnet test                   # 14 domain unit tests + 4 API integration tests (needs the DB up)
```

## Key endpoints

| Method | Route | Notes |
| --- | --- | --- |
| GET | `/api/vehicles` | Filter: `make, model, status, agingOnly, minAgeDays, maxAgeDays, search, sortBy, desc, page, pageSize` |
| GET | `/api/vehicles/aging` | Aging stock (> 90 days) |
| POST/PUT | `/api/vehicles`, `/api/vehicles/{id}` | Add / update a vehicle |
| GET/POST | `/api/vehicles/{id}/actions` | List / log actions |
| PUT | `/api/actions/{id}` | Update an action's status |
| GET | `/api/vehicles/{id}/effective-strategy` | Resolved strategy (Vehicle > VehicleType > Factory) |
| GET/POST/PUT/DELETE | `/api/strategies` | Manage strategies at any scope |
| GET | `/api/dashboard/summary` | KPIs, top sales, stock-by-make |
| Hub | `/hubs/inventory` | SignalR: `VehicleChanged`, `ActionChanged`, `DashboardChanged` |

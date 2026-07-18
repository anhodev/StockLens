---
name: inventory-domain-expert
description: Domain expert for dealership vehicle inventory in StockLens: aging stock, vehicle lifecycle, business-strategy scope resolution, actionable insights, and dashboard KPIs. Apply when modelling, naming, or reasoning about inventory business rules and metrics.
---

# Inventory Domain Expert

Authoritative reference for StockLens business rules so features use consistent
terminology and correct calculations. Use domain language, not generic names.

Defer to the codebase when it and this document disagree; if a rule here is stale,
flag it rather than silently following it.

---

# Core Concepts

## Vehicle

A unit of inventory. Key attributes: `Vin`, `Make`, `Model`, `Year`, `Trim`,
`Color`, `Mileage`, `ListPrice`, `Cost`, `Status`, `AcquiredDate`, `SoldDate`.

Derived (never stored): `DaysInInventory`, `IsAgingStock`. Compute these from the
entity relative to an `asOf` date; do not persist them.

## Vehicle Status

- `InStock`: available on the lot.
- `Reserved`: held for a buyer, still physically in inventory.
- `Sold`: left inventory; `SoldDate` is set.

A vehicle is "in inventory" when its status is not `Sold`.

---

# Aging Stock (central rule)

A vehicle is **aging stock** when it is still in inventory (status not `Sold`) and
has been held **more than 90 days** (`DaysInInventory > 90`, strictly greater; 90
exactly is not aging).

- `DaysInInventory` counts from `AcquiredDate` to the `asOf` date for in-stock
  vehicles, and from `AcquiredDate` to `SoldDate` for sold vehicles.
- The 90-day threshold is a single source of truth
  (`InventoryPolicy.AgingThresholdDays`). Never hardcode `90` elsewhere.
- Aging stock is the primary thing managers act on, so surface it prominently.

---

# Actionable Insights (VehicleAction)

Managers log and persist an action/status against a vehicle (especially aging ones).

- Action types: `PriceReductionPlanned`, `MoveToAuction`, `Promote`,
  `TransferToBranch`, `Recondition`, `Other`.
- Action lifecycle: `Open` → `InProgress` → `Done` (or `Cancelled`).
- "Open actions" on a vehicle = actions in `Open` or `InProgress`.
- Actions are persisted history; do not delete them to represent completion.
  Move them to `Done`/`Cancelled`.

---

# Business Strategy & Scope Resolution

A strategy expresses pricing/disposition intent (`TargetDaysToSell`,
`DiscountPercent`, effective dates) at one of three scopes:

- `Factory`: applies to a make. `ScopeKey = Make` (e.g. "Toyota").
- `VehicleType`: applies to a make+model. `ScopeKey = "Make|Model"`.
- `Vehicle`: applies to one vehicle. `ScopeKey = VehicleId`.

**Resolution precedence (most specific wins): Vehicle > VehicleType > Factory.**
Only active, in-effect strategies (`IsActive`, and `asOf` within
`EffectiveFrom`..`EffectiveTo`) are candidates. When several match at the same
scope, prefer the most recent `EffectiveFrom`. Resolution never merges scopes;
it selects exactly one strategy or none.

---

# Dashboard KPIs

Standard metrics and their meaning:

- **Total in stock**: count of vehicles not `Sold`.
- **Aging stock count**: in-stock vehicles over the 90-day threshold.
- **Total stock value**: sum of `ListPrice` for in-stock vehicles.
- **Average days in inventory**: mean `DaysInInventory` over in-stock vehicles.
- **Average days to sell**: mean `DaysToSell` over sales records.
- **Sold last 30 days** / **revenue last 30 days**: from sales in the trailing window.
- **Top sales**: highest-value recent sales.
- **Stock by make**: count and stock value grouped by make.

Compute counts/aggregates in the database (projection + grouping), not in memory.

---

# Terminology Guidance

Use domain terms: aging stock, days in inventory / days on lot, list price, dealer
cost, disposition, markdown, strategy scope, effective strategy. Avoid generic
names like Manager, Helper, Processor, Data, or Info for domain concepts.

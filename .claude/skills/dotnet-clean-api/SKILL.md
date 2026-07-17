---
name: dotnet-clean-api
description: Generate production-ready .NET 10 Minimal API features using Clean Architecture, Vertical Slice Architecture, Entity Framework Core, FluentValidation, and SignalR where appropriate.
---

# .NET 10 Clean API

This skill generates backend code for the Intelligent Inventory Dashboard.

Always follow the project-standards skill.

Never generate sample code.

Generate production-ready code.

---

# Technology

Always use

- .NET 10
- ASP.NET Core Minimal API
- Entity Framework Core
- SQL Server
- FluentValidation
- SignalR
- OpenTelemetry
- Serilog
- Dependency Injection

Do NOT use

- MVC Controllers
- Generic Repository
- Service Locator
- AutoMapper
- Static helper classes
- DataSet/DataTable

---

# Architectural Principles

Use

Clean Architecture

combined with

Vertical Slice Architecture

Each feature owns

- Endpoint
- Request
- Response
- Validator
- Handler

Do not share unnecessary abstractions.

---

# Feature Structure

Every feature should follow

```
Features/

    Inventory/

        GetInventory/

            Endpoint.cs

            Request.cs

            Response.cs

            Validator.cs

            Handler.cs

            Mapping.cs

            Tests.cs
```

Never place endpoints in one large file.

Never create a Controllers folder.

---

# Endpoint Style

Always use Minimal APIs.

Example

```csharp
app.MapGet("/api/inventory", Handler)
   .WithName("GetInventory")
   .WithSummary("Returns vehicle inventory")
   .Produces<IReadOnlyList<Response>>()
   .ProducesProblem(StatusCodes.Status500InternalServerError);
```

Always

- Name endpoints
- Add summaries
- Add response metadata
- Return TypedResults

---

# Request Objects

Use immutable records.

Example

```csharp
public sealed record Request(
    int Page,
    int PageSize,
    string? Search);
```

Avoid mutable DTOs.

---

# Response Objects

Responses should only expose what clients need.

Never expose EF entities.

Example

```csharp
public sealed record Response(
    Guid Id,
    string Vin,
    string Make,
    string Model,
    decimal Price,
    int DaysInStock);
```

---

# Validation

Always use FluentValidation.

One validator per request.

Example

```csharp
public sealed class Validator
    : AbstractValidator<Request>
{
    public Validator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1,100);
    }
}
```

Never validate inside handlers.

---

# Handler

Handler should contain business use case only.

Constructor injection only.

Always include

CancellationToken

ILogger

Example

```csharp
public sealed class Handler(
    InventoryDbContext db,
    ILogger<Handler> logger)
{
}
```

---

# Entity Framework

Always

Use projections

Use AsNoTracking()

Use async

Pass CancellationToken

Example

```csharp
var vehicles = await db.Vehicles
    .AsNoTracking()
    .Where(x => x.Status == VehicleStatus.Available)
    .Select(...)
    .ToListAsync(ct);
```

Avoid

Include()

unless necessary.

Avoid loading entire entities.

---

# Mapping

Prefer manual mapping.

Example

```csharp
.Select(vehicle => new Response(
    vehicle.Id,
    vehicle.Vin,
    vehicle.Make,
    vehicle.Model,
    vehicle.Price,
    vehicle.DaysInStock));
```

Avoid AutoMapper.

---

# Error Handling

Expected business failures

Return

Results

ProblemDetails

TypedResults

Unexpected exceptions

Log

Return generic error

Never expose stack traces.

---

# Logging

Use structured logging.

Good

```csharp
logger.LogInformation(
    "Vehicle {VehicleId} updated",
    request.Id);
```

Bad

```csharp
logger.LogInformation(
    $"Vehicle {request.Id} updated");
```

---

# Pagination

Always paginate collections.

Preferred

```csharp
.Skip(page * pageSize)
.Take(pageSize)
```

Include

TotalCount

Page

PageSize

---

# Filtering

Filtering belongs in query.

Avoid filtering in memory.

Good

```csharp
.Where(v => v.Make == request.Make)
```

Bad

```csharp
.ToList()
.Where(...)
```

---

# Sorting

Support explicit sorting.

Never rely on database order.

---

# Transactions

Only create transactions for

multiple writes.

Do not wrap read operations.

---

# SignalR

When data changes

Evaluate whether connected dashboards
should receive updates.

If yes

Publish minimal update events.

Good

```
VehiclePriceUpdated

InventoryUpdated

InventoryStatisticsUpdated
```

Bad

```
EntireInventoryListChanged
```

Broadcast only required data.

---

# Performance

Always think about

Database round trips

Indexes

Projection

Memory allocation

Payload size

Avoid

ToList()

before filtering.

Avoid multiple SaveChanges().

Avoid unnecessary allocations.

---

# Security

Never trust client input.

Always validate.

Never expose internal IDs unless intended.

Never concatenate SQL.

Always use EF Core parameterization.

---

# OpenTelemetry

Every handler should support tracing.

Log important operations.

Avoid excessive logging.

---

# XML Documentation

Public APIs

should contain XML documentation.

Complex logic

should explain WHY.

---

# Unit Testing

Every feature should include tests.

Test

Happy path

Validation

Not found

Unauthorized

Edge cases

---

# Integration Tests

Endpoints should be tested using

WebApplicationFactory.

Avoid mocking the entire application.

---

# Code Quality

Generated code should

Compile

Be nullable-safe

Use async everywhere

Use file-scoped namespaces

Use primary constructors

Use records where appropriate

Use collection expressions

Use expression-bodied members when clearer

Prefer readonly

Prefer immutable types

---

# Anti Patterns

Never generate

Controllers

Repository<T>

UnitOfWork

Helpers.cs

Extensions containing business logic

God services

Static state

Business logic inside endpoints

Business logic inside EF entities

---

# Before Returning Code

Verify

✅ Clean Architecture respected

✅ Vertical Slice respected

✅ Minimal API used

✅ FluentValidation included

✅ CancellationToken propagated

✅ Async everywhere

✅ Structured logging

✅ EF queries optimized

✅ Manual mapping

✅ Typed Results

✅ SignalR considered

✅ Unit tests included

If any item fails, fix it before producing the final code.
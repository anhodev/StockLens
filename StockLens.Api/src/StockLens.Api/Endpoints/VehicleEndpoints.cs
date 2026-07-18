using Microsoft.EntityFrameworkCore;
using StockLens.Api.Filters;
using StockLens.Application.Abstractions;
using StockLens.Application.Dtos;
using StockLens.Application.Mapping;
using StockLens.Application.Services;
using StockLens.Domain.Common;
using StockLens.Domain.Entities;
using StockLens.Domain.Enums;

namespace StockLens.Api.Endpoints;

public static class VehicleEndpoints
{
    private const string LogCategory = "StockLens.Api.Vehicles";
    private const string LikeEscape = "\\";

    /// <summary>Escapes LIKE wildcards so a term such as "50%" is matched literally.</summary>
    private static string EscapeLike(string term) => term
        .Replace("\\", "\\\\")
        .Replace("%", "\\%")
        .Replace("_", "\\_");

    public static IEndpointRouteBuilder MapVehicleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/vehicles").WithTags("Vehicles");

        group.MapGet("/", GetVehicles);
        group.MapGet("/aging", GetAgingStock);
        group.MapGet("/{id:guid}", GetVehicle);
        group.MapGet("/{id:guid}/effective-strategy", GetEffectiveStrategy);
        group.MapPost("/", CreateVehicle).WithValidation<CreateVehicleRequest>();
        group.MapPut("/{id:guid}", UpdateVehicle).WithValidation<UpdateVehicleRequest>();
        group.MapPost("/{id:guid}/status", ChangeStatus).WithValidation<ChangeVehicleStatusRequest>();
        group.MapGet("/{id:guid}/status-history", GetStatusHistory);

        return app;
    }

    /// <summary>Moves a vehicle to a new status, recording the evidence the move required.</summary>
    private static async Task<IResult> ChangeStatus(
        Guid id, ChangeVehicleStatusRequest req, VehicleStatusService service,
        IInventoryNotifier notifier, DashboardService dashboard, CancellationToken ct)
    {
        var result = await service.ChangeStatusAsync(id, req, ct);

        switch (result.Outcome)
        {
            case StatusChangeOutcome.VehicleNotFound:
                return Results.NotFound();
            case StatusChangeOutcome.VehicleSold:
                return Results.Problem(result.Message, statusCode: StatusCodes.Status422UnprocessableEntity);
            case StatusChangeOutcome.SalespersonNotFound:
            case StatusChangeOutcome.AlreadyInStatus:
                return Results.Problem(result.Message, statusCode: StatusCodes.Status400BadRequest);
        }

        // Status drives aging, stock counts and (when sold) the sales trend.
        await notifier.VehicleChangedAsync(result.Vehicle!, ct);
        await notifier.DashboardChangedAsync(await dashboard.GetSummaryAsync(ct), ct);

        return Results.Ok(result.Vehicle);
    }

    private static async Task<IResult> GetStatusHistory(
        Guid id, IApplicationDbContext db, VehicleStatusService service, CancellationToken ct)
    {
        if (!await db.Vehicles.AnyAsync(v => v.Id == id, ct))
            return Results.NotFound();

        return Results.Ok(await service.GetHistoryAsync(id, ct));
    }

    private static async Task<IResult> GetVehicles(
        IApplicationDbContext db, VehiclePricingService pricing, [AsParameters] VehicleFilter filter, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = db.Vehicles.AsNoTracking().Include(v => v.Actions).Include(v => v.Salesperson).AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Make))
            query = query.Where(v => v.Make == filter.Make);
        if (!string.IsNullOrWhiteSpace(filter.Model))
            query = query.Where(v => v.Model == filter.Model);
        if (filter.Status.HasValue)
            query = query.Where(v => v.Status == filter.Status);
        // Free-text search matches the way a vehicle reads on screen ("2020 Ford Escape"):
        // every whitespace-separated term must appear in at least one field, so terms
        // narrow the result set rather than each having to match a single column alone.
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var terms = filter.Search.Split(' ', StringSplitOptions.RemoveEmptyEntries
                                                 | StringSplitOptions.TrimEntries);
            foreach (var term in terms)
            {
                var pattern = $"%{EscapeLike(term)}%";
                query = query.Where(v =>
                    EF.Functions.ILike(v.Vin, pattern, LikeEscape) ||
                    EF.Functions.ILike(v.Make, pattern, LikeEscape) ||
                    EF.Functions.ILike(v.Model, pattern, LikeEscape) ||
                    EF.Functions.ILike(v.Trim!, pattern, LikeEscape) ||
                    EF.Functions.ILike(v.Color!, pattern, LikeEscape) ||
                    EF.Functions.ILike(v.Year.ToString(), pattern, LikeEscape));
            }
        }

        // Age filters translate to acquisition-date bounds relative to today.
        if (filter.AgingOnly == true)
            query = query.Where(v => v.Status != VehicleStatus.Sold
                && v.AcquiredDate < today.AddDays(-InventoryPolicy.AgingThresholdDays));
        if (filter.MinAgeDays.HasValue)
            query = query.Where(v => v.AcquiredDate <= today.AddDays(-filter.MinAgeDays.Value));
        if (filter.MaxAgeDays.HasValue)
            query = query.Where(v => v.AcquiredDate >= today.AddDays(-filter.MaxAgeDays.Value));

        query = (filter.SortBy.ToLowerInvariant(), filter.Desc) switch
        {
            ("price", true) => query.OrderByDescending(v => v.ListPrice),
            ("price", false) => query.OrderBy(v => v.ListPrice),
            ("make", true) => query.OrderByDescending(v => v.Make).ThenByDescending(v => v.Model),
            ("make", false) => query.OrderBy(v => v.Make).ThenBy(v => v.Model),
            // "age" desc == oldest first (earliest acquisition date).
            ("age", false) => query.OrderByDescending(v => v.AcquiredDate),
            _ => query.OrderBy(v => v.AcquiredDate),
        };

        var total = await query.CountAsync(ct);
        var page = Math.Max(1, filter.Page);
        var size = Math.Clamp(filter.PageSize, 1, 200);

        var items = await query.Skip((page - 1) * size).Take(size).ToListAsync(ct);
        var discounts = await pricing.ResolveDiscountsAsync(items, today, ct);
        var dtos = items.Select(v => v.ToDto(today, discounts.GetValueOrDefault(v.Id))).ToList();

        return Results.Ok(new PagedResult<VehicleDto>(dtos, total, page, size));
    }

    private static async Task<IResult> GetAgingStock(
        IApplicationDbContext db, VehiclePricingService pricing, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var cutoff = today.AddDays(-InventoryPolicy.AgingThresholdDays);

        var items = await db.Vehicles
            .AsNoTracking()
            .Include(v => v.Actions)
            .Include(v => v.Salesperson)
            .Where(v => v.Status != VehicleStatus.Sold && v.AcquiredDate < cutoff)
            .OrderBy(v => v.AcquiredDate)
            .ToListAsync(ct);

        var discounts = await pricing.ResolveDiscountsAsync(items, today, ct);
        return Results.Ok(items.Select(v => v.ToDto(today, discounts.GetValueOrDefault(v.Id))).ToList());
    }

    private static async Task<IResult> GetVehicle(
        Guid id, IApplicationDbContext db, VehiclePricingService pricing, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var v = await db.Vehicles.AsNoTracking().Include(x => x.Actions).Include(x => x.Salesperson)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (v is null) return Results.NotFound();

        var discount = await pricing.ResolveDiscountAsync(v, today, ct);
        return Results.Ok(v.ToDto(today, discount));
    }

    private static async Task<IResult> GetEffectiveStrategy(
        Guid id, IApplicationDbContext db, IStrategyResolver resolver, CancellationToken ct)
    {
        var v = await db.Vehicles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (v is null) return Results.NotFound();
        if (v.Status == VehicleStatus.Sold) return Results.NoContent();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var typeKey = BusinessStrategy.VehicleTypeKey(v.Make, v.Model);
        var vehicleKey = v.Id.ToString();

        // Only load candidate strategies that could possibly match this vehicle.
        var candidates = await db.Strategies
            .AsNoTracking()
            .Where(s => (s.Scope == StrategyScope.Factory && s.ScopeKey == v.Make)
                     || (s.Scope == StrategyScope.VehicleType && s.ScopeKey == typeKey)
                     || (s.Scope == StrategyScope.Vehicle && s.ScopeKey == vehicleKey))
            .ToListAsync(ct);

        var result = resolver.Resolve(v, candidates, today);
        return result is null ? Results.NoContent() : Results.Ok(result);
    }

    private static async Task<IResult> CreateVehicle(
        CreateVehicleRequest req, IApplicationDbContext db, IInventoryNotifier notifier,
        DashboardService dashboard, VehiclePricingService pricing, ILoggerFactory loggerFactory, CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger(LogCategory);

        if (await db.Vehicles.AnyAsync(v => v.Vin == req.Vin, ct))
            return Results.Conflict($"A vehicle with VIN '{req.Vin}' already exists.");

        var vehicle = new Vehicle
        {
            Vin = req.Vin,
            Make = req.Make,
            Model = req.Model,
            Year = req.Year,
            Trim = req.Trim,
            Color = req.Color,
            Mileage = req.Mileage,
            BodyType = req.BodyType,
            ListPrice = req.ListPrice,
            Cost = req.Cost,
            Status = req.Status,
            AcquiredDate = req.AcquiredDate,
        };

        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Vehicle {VehicleId} created (VIN {Vin}, {Make} {Model})",
            vehicle.Id, vehicle.Vin, vehicle.Make, vehicle.Model);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dto = vehicle.ToDto(today, await pricing.ResolveDiscountAsync(vehicle, today, ct));
        await notifier.VehicleChangedAsync(dto, ct);
        await notifier.DashboardChangedAsync(await dashboard.GetSummaryAsync(ct), ct);

        return Results.Created($"/api/vehicles/{vehicle.Id}", dto);
    }

    private static async Task<IResult> UpdateVehicle(
        Guid id, UpdateVehicleRequest req, IApplicationDbContext db, IInventoryNotifier notifier,
        DashboardService dashboard, VehiclePricingService pricing, ILoggerFactory loggerFactory, CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger(LogCategory);

        var v = await db.Vehicles.Include(x => x.Actions).Include(x => x.Salesperson)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (v is null) return Results.NotFound();

        v.Make = req.Make;
        v.Model = req.Model;
        v.Year = req.Year;
        v.Trim = req.Trim;
        v.Color = req.Color;
        v.Mileage = req.Mileage;
        v.BodyType = req.BodyType;
        v.ListPrice = req.ListPrice;
        v.Cost = req.Cost;
        v.Status = req.Status;
        v.AcquiredDate = req.AcquiredDate;
        v.SoldDate = req.SoldDate;
        v.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Vehicle {VehicleId} updated (status {Status})", v.Id, v.Status);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dto = v.ToDto(today, await pricing.ResolveDiscountAsync(v, today, ct));
        await notifier.VehicleChangedAsync(dto, ct);
        await notifier.DashboardChangedAsync(await dashboard.GetSummaryAsync(ct), ct);

        return Results.Ok(dto);
    }
}

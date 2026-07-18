using Microsoft.EntityFrameworkCore;
using StockLens.Api.Filters;
using StockLens.Application.Abstractions;
using StockLens.Application.Dtos;
using StockLens.Application.Mapping;
using StockLens.Application.Services;
using StockLens.Domain.Entities;
using StockLens.Domain.Enums;

namespace StockLens.Api.Endpoints;

public static class StrategyEndpoints
{
    private const string LogCategory = "StockLens.Api.Strategies";

    public static IEndpointRouteBuilder MapStrategyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/strategies").WithTags("Strategies");

        group.MapGet("/", GetStrategies);
        group.MapGet("/scope-options", GetScopeOptions);
        group.MapGet("/{id:guid}", GetStrategy);
        group.MapPost("/", CreateStrategy).WithValidation<UpsertStrategyRequest>();
        group.MapPut("/{id:guid}", UpdateStrategy).WithValidation<UpsertStrategyRequest>();
        group.MapDelete("/{id:guid}", DeleteStrategy);

        return app;
    }

    private static async Task<IResult> GetStrategies(
        IApplicationDbContext db, StrategyScope? scope, string? scopeKey, CancellationToken ct)
    {
        var query = db.Strategies.AsNoTracking().AsQueryable();
        if (scope.HasValue) query = query.Where(s => s.Scope == scope);
        if (!string.IsNullOrWhiteSpace(scopeKey)) query = query.Where(s => s.ScopeKey == scopeKey);

        var list = await query
            .OrderBy(s => s.Scope).ThenBy(s => s.ScopeKey)
            .ToListAsync(ct);

        return Results.Ok(list.Select(s => s.ToDto()).ToList());
    }

    private static async Task<IResult> GetScopeOptions(IApplicationDbContext db, CancellationToken ct)
    {
        var vehicles = await db.Vehicles
            .AsNoTracking()
            .Where(v => v.Status != VehicleStatus.Sold)
            .Select(v => new { v.Id, v.Vin, v.Make, v.Model, v.Year })
            .OrderBy(v => v.Make).ThenBy(v => v.Model).ThenBy(v => v.Year)
            .ToListAsync(ct);

        var factories = vehicles
            .Select(v => v.Make)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        var vehicleTypes = vehicles
            .Select(v => BusinessStrategy.VehicleTypeKey(v.Make, v.Model))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        var vehicleSummaries = vehicles
            .Select(v => new VehicleSummaryDto(v.Id.ToString(), v.Vin, v.Make, v.Model, v.Year))
            .ToList();

        return Results.Ok(new StrategyScopeOptionsDto(factories, vehicleTypes, vehicleSummaries));
    }

    private static async Task<IResult> GetStrategy(Guid id, IApplicationDbContext db, CancellationToken ct)
    {
        var s = await db.Strategies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return s is null ? Results.NotFound() : Results.Ok(s.ToDto());
    }

    private static async Task<IResult> CreateStrategy(
        UpsertStrategyRequest req, IApplicationDbContext db,
        IInventoryNotifier notifier, DashboardService dashboard, ILoggerFactory loggerFactory, CancellationToken ct)
    {
        var s = new BusinessStrategy
        {
            Scope = req.Scope,
            ScopeKey = req.ScopeKey,
            Name = req.Name,
            Description = req.Description,
            TargetDaysToSell = req.TargetDaysToSell,
            DiscountPercent = req.DiscountPercent,
            IsActive = req.IsActive,
            EffectiveFrom = req.EffectiveFrom,
            EffectiveTo = req.EffectiveTo,
        };

        db.Strategies.Add(s);
        await db.SaveChangesAsync(ct);

        loggerFactory.CreateLogger(LogCategory).LogInformation(
            "Strategy {StrategyId} created ({Scope} {ScopeKey})", s.Id, s.Scope, s.ScopeKey);

        var dto = s.ToDto();
        await notifier.StrategyChangedAsync(dto, ct);
        await notifier.DashboardChangedAsync(await dashboard.GetSummaryAsync(ct), ct);
        return Results.Created($"/api/strategies/{s.Id}", dto);
    }

    private static async Task<IResult> UpdateStrategy(
        Guid id, UpsertStrategyRequest req, IApplicationDbContext db,
        IInventoryNotifier notifier, DashboardService dashboard, ILoggerFactory loggerFactory, CancellationToken ct)
    {
        var s = await db.Strategies.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s is null) return Results.NotFound();

        s.Scope = req.Scope;
        s.ScopeKey = req.ScopeKey;
        s.Name = req.Name;
        s.Description = req.Description;
        s.TargetDaysToSell = req.TargetDaysToSell;
        s.DiscountPercent = req.DiscountPercent;
        s.IsActive = req.IsActive;
        s.EffectiveFrom = req.EffectiveFrom;
        s.EffectiveTo = req.EffectiveTo;
        s.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);

        loggerFactory.CreateLogger(LogCategory).LogInformation("Strategy {StrategyId} updated", s.Id);

        var dto = s.ToDto();
        await notifier.StrategyChangedAsync(dto, ct);
        await notifier.DashboardChangedAsync(await dashboard.GetSummaryAsync(ct), ct);
        return Results.Ok(dto);
    }

    private static async Task<IResult> DeleteStrategy(
        Guid id, IApplicationDbContext db,
        IInventoryNotifier notifier, DashboardService dashboard, ILoggerFactory loggerFactory, CancellationToken ct)
    {
        var s = await db.Strategies.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s is null) return Results.NotFound();

        var dto = s.ToDto();
        db.Strategies.Remove(s);
        await db.SaveChangesAsync(ct);

        loggerFactory.CreateLogger(LogCategory).LogInformation("Strategy {StrategyId} deleted", id);

        await notifier.StrategyChangedAsync(dto, ct);
        await notifier.DashboardChangedAsync(await dashboard.GetSummaryAsync(ct), ct);
        return Results.NoContent();
    }
}

using Microsoft.EntityFrameworkCore;
using StockLens.Application.Abstractions;
using StockLens.Domain.Entities;

namespace StockLens.Application.Services;

/// <summary>
/// Resolves the effective business-strategy discount for vehicles, so list and detail
/// prices reflect any applicable strategy. The discount is derived on read — never stored —
/// so it stays correct when a strategy is added, edited, or expires.
/// </summary>
public class VehiclePricingService
{
    private readonly IApplicationDbContext _db;
    private readonly IStrategyResolver _resolver;

    public VehiclePricingService(IApplicationDbContext db, IStrategyResolver resolver)
    {
        _db = db;
        _resolver = resolver;
    }

    /// <summary>
    /// Effective discount % per vehicle (null when no strategy applies). Loads all strategies
    /// once and resolves each vehicle in memory, so a page of vehicles costs a single query.
    /// </summary>
    public async Task<IReadOnlyDictionary<Guid, decimal?>> ResolveDiscountsAsync(
        IReadOnlyCollection<Vehicle> vehicles, DateOnly asOf, CancellationToken ct = default)
    {
        var result = new Dictionary<Guid, decimal?>(vehicles.Count);
        if (vehicles.Count == 0) return result;

        var strategies = await _db.Strategies.AsNoTracking().ToListAsync(ct);
        foreach (var v in vehicles)
        {
            // The resolver picks the most specific in-effect strategy (Vehicle > VehicleType > Factory).
            var effective = _resolver.Resolve(v, strategies, asOf);
            result[v.Id] = effective?.Strategy.DiscountPercent;
        }

        return result;
    }

    /// <summary>Convenience for a single vehicle.</summary>
    public async Task<decimal?> ResolveDiscountAsync(Vehicle vehicle, DateOnly asOf, CancellationToken ct = default)
    {
        var map = await ResolveDiscountsAsync(new[] { vehicle }, asOf, ct);
        return map.GetValueOrDefault(vehicle.Id);
    }
}

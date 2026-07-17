using StockLens.Application.Abstractions;
using StockLens.Application.Dtos;
using StockLens.Application.Mapping;
using StockLens.Domain.Entities;
using StockLens.Domain.Enums;

namespace StockLens.Application.Strategies;

/// <summary>
/// Chooses the most specific active, in-effect strategy for a vehicle.
/// Precedence: Vehicle &gt; VehicleType (make+model) &gt; Factory (make).
/// </summary>
public class StrategyResolver : IStrategyResolver
{
    public EffectiveStrategyDto? Resolve(Vehicle vehicle, IEnumerable<BusinessStrategy> candidates, DateOnly asOf)
    {
        var vehicleKey = vehicle.Id.ToString();
        var typeKey = BusinessStrategy.VehicleTypeKey(vehicle.Make, vehicle.Model);
        var factoryKey = vehicle.Make;

        // Evaluate scopes from most specific to least; first hit wins.
        foreach (var scope in new[] { StrategyScope.Vehicle, StrategyScope.VehicleType, StrategyScope.Factory })
        {
            var key = scope switch
            {
                StrategyScope.Vehicle => vehicleKey,
                StrategyScope.VehicleType => typeKey,
                _ => factoryKey
            };

            var match = candidates
                .Where(s => s.Scope == scope
                            && string.Equals(s.ScopeKey, key, StringComparison.OrdinalIgnoreCase)
                            && IsInEffect(s, asOf))
                .OrderByDescending(s => s.EffectiveFrom)
                .FirstOrDefault();

            if (match is not null)
                return new EffectiveStrategyDto(vehicle.Id, match.ToDto(), scope);
        }

        return null;
    }

    private static bool IsInEffect(BusinessStrategy s, DateOnly asOf) =>
        s.IsActive && s.EffectiveFrom <= asOf && (s.EffectiveTo is null || s.EffectiveTo >= asOf);
}

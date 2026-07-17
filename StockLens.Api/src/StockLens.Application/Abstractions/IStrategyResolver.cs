using StockLens.Application.Dtos;
using StockLens.Domain.Entities;

namespace StockLens.Application.Abstractions;

/// <summary>Resolves which business strategy applies to a vehicle, honouring scope precedence.</summary>
public interface IStrategyResolver
{
    /// <summary>
    /// Picks the most specific active strategy for the vehicle from the candidate set:
    /// Vehicle &gt; VehicleType &gt; Factory. Returns null when none apply.
    /// </summary>
    EffectiveStrategyDto? Resolve(Vehicle vehicle, IEnumerable<BusinessStrategy> candidates, DateOnly asOf);
}

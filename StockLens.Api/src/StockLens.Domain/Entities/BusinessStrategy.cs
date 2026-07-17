using StockLens.Domain.Enums;

namespace StockLens.Domain.Entities;

/// <summary>
/// A pricing / disposition strategy that applies at one of three scopes:
/// a whole factory (make), a vehicle type (make+model), or a single vehicle.
/// See <see cref="StrategyScope"/> for how <see cref="ScopeKey"/> is encoded.
/// </summary>
public class BusinessStrategy
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public StrategyScope Scope { get; set; }

    /// <summary>
    /// Identifies the target within the scope: Make, "Make|Model", or a VehicleId string.
    /// </summary>
    public string ScopeKey { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Target number of days to sell before escalation.</summary>
    public int? TargetDaysToSell { get; set; }

    /// <summary>Planned/standing discount percentage for matching vehicles.</summary>
    public decimal? DiscountPercent { get; set; }

    public bool IsActive { get; set; } = true;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Builds the canonical scope key for a make+model vehicle type.</summary>
    public static string VehicleTypeKey(string make, string model) => $"{make}|{model}";
}

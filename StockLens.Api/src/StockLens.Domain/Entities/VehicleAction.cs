using StockLens.Domain.Enums;

namespace StockLens.Domain.Entities;

/// <summary>
/// A manager-logged status or proposed action for a vehicle
/// (e.g. "Price Reduction Planned"). Persisted so it survives across sessions.
/// </summary>
public class VehicleAction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public ActionType ActionType { get; set; } = ActionType.PriceReductionPlanned;
    public ActionStatus Status { get; set; } = ActionStatus.Open;

    /// <summary>Free-text detail, e.g. "Drop by $1,500 after the long weekend".</summary>
    public string? Note { get; set; }

    /// <summary>Who logged the action (no auth yet, so a plain manager name/label).</summary>
    public string CreatedBy { get; set; } = "manager";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

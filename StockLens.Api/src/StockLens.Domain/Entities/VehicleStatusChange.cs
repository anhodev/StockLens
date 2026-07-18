using StockLens.Domain.Enums;

namespace StockLens.Domain.Entities;

/// <summary>
/// An audited move of a vehicle from one <see cref="VehicleStatus"/> to another, capturing
/// the evidence the transition required (deposit taken, reason given, salesperson credited).
/// Rows are append-only: they are the record of why a vehicle's state changed.
/// </summary>
public class VehicleStatusChange
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public VehicleStatus FromStatus { get; set; }
    public VehicleStatus ToStatus { get; set; }

    /// <summary>Why the vehicle moved. Required when moving to Hold or back to Open.</summary>
    public string? Reason { get; set; }

    /// <summary>Deposit taken. Required when moving to Deposited.</summary>
    public decimal? DepositAmount { get; set; }

    /// <summary>Agreed price. Captured when moving to Sold.</summary>
    public decimal? SalePrice { get; set; }

    /// <summary>Salesperson credited. Required when moving to Deposited or Sold.</summary>
    public Guid? SalespersonId { get; set; }
    public Salesperson? Salesperson { get; set; }

    /// <summary>The business date the change takes effect (the sale date when moving to Sold).</summary>
    public DateOnly EffectiveDate { get; set; }

    public string ChangedBy { get; set; } = "manager";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

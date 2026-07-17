namespace StockLens.Domain.Entities;

/// <summary>A completed sale, used to surface "top sales" on the dashboard.</summary>
public class SalesRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public decimal SalePrice { get; set; }
    public DateOnly SoldDate { get; set; }

    /// <summary>Days from acquisition to sale — a key inventory-health metric.</summary>
    public int DaysToSell { get; set; }

    public string SoldBy { get; set; } = "manager";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

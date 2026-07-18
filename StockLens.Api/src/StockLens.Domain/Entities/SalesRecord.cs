namespace StockLens.Domain.Entities;

/// <summary>A completed sale, used to surface "top sales" and sales performance on the dashboard.</summary>
public class SalesRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    /// <summary>The salesperson credited with the sale.</summary>
    public Guid SalespersonId { get; set; }
    public Salesperson? Salesperson { get; set; }

    public decimal SalePrice { get; set; }
    public DateOnly SoldDate { get; set; }

    /// <summary>Days from acquisition to sale — a key inventory-health metric.</summary>
    public int DaysToSell { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

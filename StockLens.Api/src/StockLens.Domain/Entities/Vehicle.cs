using StockLens.Domain.Common;
using StockLens.Domain.Enums;

namespace StockLens.Domain.Entities;

/// <summary>A vehicle held in a dealership's inventory.</summary>
public class Vehicle
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Vin { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string? Trim { get; set; }
    public string? Color { get; set; }
    public int Mileage { get; set; }

    /// <summary>Current asking price.</summary>
    public decimal ListPrice { get; set; }

    /// <summary>Acquisition cost to the dealership; used for margin/insight.</summary>
    public decimal Cost { get; set; }

    public VehicleStatus Status { get; set; } = VehicleStatus.InStock;

    /// <summary>Date the vehicle entered inventory.</summary>
    public DateOnly AcquiredDate { get; set; }

    /// <summary>Date the vehicle was sold, if it has been.</summary>
    public DateOnly? SoldDate { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<VehicleAction> Actions { get; set; } = new List<VehicleAction>();

    /// <summary>
    /// Number of days the vehicle has been (or was) in inventory, relative to the supplied date.
    /// For sold vehicles this is the acquisition-to-sale span.
    /// </summary>
    public int DaysInInventory(DateOnly asOf)
    {
        var end = Status == VehicleStatus.Sold && SoldDate.HasValue ? SoldDate.Value : asOf;
        var days = end.DayNumber - AcquiredDate.DayNumber;
        return days < 0 ? 0 : days;
    }

    /// <summary>True when the vehicle is still in stock and has aged past the policy threshold.</summary>
    public bool IsAgingStock(DateOnly asOf) =>
        Status != VehicleStatus.Sold && DaysInInventory(asOf) > InventoryPolicy.AgingThresholdDays;
}

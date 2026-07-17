using StockLens.Domain.Entities;
using StockLens.Domain.Enums;

namespace StockLens.Domain.Tests;

public class VehicleAgingTests
{
    private static readonly DateOnly Today = new(2026, 7, 15);

    private static Vehicle InStock(int ageDays) => new()
    {
        Status = VehicleStatus.InStock,
        AcquiredDate = Today.AddDays(-ageDays),
    };

    [Theory]
    [InlineData(0, false)]
    [InlineData(90, false)]   // exactly 90 is NOT aging (rule is > 90)
    [InlineData(91, true)]
    [InlineData(200, true)]
    public void IsAgingStock_reflects_threshold(int ageDays, bool expected)
    {
        var v = InStock(ageDays);
        Assert.Equal(expected, v.IsAgingStock(Today));
    }

    [Fact]
    public void DaysInInventory_counts_from_acquisition()
    {
        var v = InStock(45);
        Assert.Equal(45, v.DaysInInventory(Today));
    }

    [Fact]
    public void Sold_vehicle_is_never_aging_stock()
    {
        var v = new Vehicle
        {
            Status = VehicleStatus.Sold,
            AcquiredDate = Today.AddDays(-300),
            SoldDate = Today.AddDays(-10),
        };
        Assert.False(v.IsAgingStock(Today));
    }

    [Fact]
    public void Sold_vehicle_days_in_inventory_uses_sold_date()
    {
        var v = new Vehicle
        {
            Status = VehicleStatus.Sold,
            AcquiredDate = Today.AddDays(-100),
            SoldDate = Today.AddDays(-40),
        };
        Assert.Equal(60, v.DaysInInventory(Today));
    }

    [Fact]
    public void Reserved_vehicle_can_be_aging()
    {
        var v = new Vehicle { Status = VehicleStatus.Reserved, AcquiredDate = Today.AddDays(-120) };
        Assert.True(v.IsAgingStock(Today));
    }
}

namespace StockLens.Domain.Common;

/// <summary>Central place for inventory business rules that are not tied to a single entity instance.</summary>
public static class InventoryPolicy
{
    /// <summary>A vehicle in stock for more than this many days is considered "aging stock".</summary>
    public const int AgingThresholdDays = 90;
}

namespace StockLens.Domain.Enums;

/// <summary>
/// Lifecycle state of a vehicle in the dealership's inventory. Every state except
/// <see cref="Sold"/> still counts as being on the lot, and therefore still ages.
/// </summary>
public enum VehicleStatus
{
    /// <summary>Available on the lot with no customer committed to it.</summary>
    Open = 0,

    /// <summary>A customer has paid a deposit against the vehicle.</summary>
    Deposited = 1,

    /// <summary>Withheld from sale for a stated reason (recon, transfer, manager hold).</summary>
    Hold = 2,

    /// <summary>Sold and off the lot.</summary>
    Sold = 3
}

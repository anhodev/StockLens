namespace StockLens.Domain.Enums;

/// <summary>
/// Physical body style of a vehicle. Drives how stock is presented and filtered:
/// dealership managers think in body styles when moving aging inventory.
/// </summary>
public enum BodyType
{
    Sedan = 0,
    Suv = 1,
    Truck = 2,
    Hatchback = 3,
    Coupe = 4,
    Van = 5,
    Wagon = 6
}

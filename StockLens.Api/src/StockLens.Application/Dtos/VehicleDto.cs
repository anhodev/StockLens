using StockLens.Domain.Enums;

namespace StockLens.Application.Dtos;

public record VehicleDto(
    Guid Id,
    string Vin,
    string Make,
    string Model,
    int Year,
    string? Trim,
    string? Color,
    int Mileage,
    BodyType BodyType,
    decimal ListPrice,
    /// <summary>Effective discount % from the applied business strategy, if any.</summary>
    decimal? DiscountPercent,
    /// <summary>List price after the effective strategy discount; equals ListPrice when none applies.</summary>
    decimal NetPrice,
    decimal Cost,
    VehicleStatus Status,
    decimal? DepositAmount,
    string? SalespersonName,
    DateOnly AcquiredDate,
    DateOnly? SoldDate,
    int DaysInInventory,
    bool IsAgingStock,
    int OpenActionCount);

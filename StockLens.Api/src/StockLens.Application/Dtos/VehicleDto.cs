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
    decimal Cost,
    VehicleStatus Status,
    DateOnly AcquiredDate,
    DateOnly? SoldDate,
    int DaysInInventory,
    bool IsAgingStock,
    int OpenActionCount);

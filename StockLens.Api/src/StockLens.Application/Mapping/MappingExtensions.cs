using StockLens.Application.Dtos;
using StockLens.Domain.Entities;
using StockLens.Domain.Enums;

namespace StockLens.Application.Mapping;

/// <summary>Hand-rolled mapping between domain entities and DTOs (no reflection/AutoMapper).</summary>
public static class MappingExtensions
{
    public static VehicleDto ToDto(this Vehicle v, DateOnly asOf)
    {
        var openActions = v.Actions?.Count(a => a.Status is ActionStatus.Open or ActionStatus.InProgress) ?? 0;
        return new VehicleDto(
            v.Id, v.Vin, v.Make, v.Model, v.Year, v.Trim, v.Color, v.Mileage, v.BodyType,
            v.ListPrice, v.Cost, v.Status, v.AcquiredDate, v.SoldDate,
            v.DaysInInventory(asOf), v.IsAgingStock(asOf), openActions);
    }

    public static VehicleActionDto ToDto(this VehicleAction a) => new(
        a.Id, a.VehicleId, a.ActionType, a.Status, a.Note, a.CreatedBy, a.CreatedAt, a.UpdatedAt);

    public static BusinessStrategyDto ToDto(this BusinessStrategy s) => new(
        s.Id, s.Scope, s.ScopeKey, s.Name, s.Description, s.TargetDaysToSell,
        s.DiscountPercent, s.IsActive, s.EffectiveFrom, s.EffectiveTo);

    public static TopSaleDto ToTopSaleDto(this SalesRecord r) => new(
        r.VehicleId,
        r.Vehicle?.Make ?? string.Empty,
        r.Vehicle?.Model ?? string.Empty,
        r.Vehicle?.Year ?? 0,
        r.SalePrice, r.SoldDate, r.DaysToSell);
}

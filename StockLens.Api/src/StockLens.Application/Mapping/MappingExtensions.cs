using StockLens.Application.Dtos;
using StockLens.Domain.Entities;
using StockLens.Domain.Enums;

namespace StockLens.Application.Mapping;

/// <summary>Hand-rolled mapping between domain entities and DTOs (no reflection/AutoMapper).</summary>
public static class MappingExtensions
{
    /// <summary>
    /// Maps a vehicle to its DTO. Pass the effective strategy discount to apply it to the
    /// net price; the discount is derived (never stored) so it always reflects the current strategy.
    /// </summary>
    public static VehicleDto ToDto(this Vehicle v, DateOnly asOf, decimal? discountPercent = null)
    {
        var openActions = v.Actions?.Count(a => a.Status is ActionStatus.Open or ActionStatus.InProgress) ?? 0;
        var netPrice = discountPercent is > 0
            ? Math.Round(v.ListPrice * (1 - discountPercent.Value / 100m), 2, MidpointRounding.AwayFromZero)
            : v.ListPrice;
        return new VehicleDto(
            v.Id, v.Vin, v.Make, v.Model, v.Year, v.Trim, v.Color, v.Mileage, v.BodyType,
            v.ListPrice, discountPercent, netPrice, v.Cost, v.Status, v.DepositAmount, v.Salesperson?.FullName,
            v.AcquiredDate, v.SoldDate,
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
        r.SalePrice, r.SoldDate, r.DaysToSell,
        r.Salesperson?.FullName ?? "—");
}

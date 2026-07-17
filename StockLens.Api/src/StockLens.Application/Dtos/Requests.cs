using StockLens.Domain.Enums;

namespace StockLens.Application.Dtos;

public record CreateVehicleRequest(
    string Vin,
    string Make,
    string Model,
    int Year,
    string? Trim,
    string? Color,
    int Mileage,
    decimal ListPrice,
    decimal Cost,
    DateOnly AcquiredDate,
    VehicleStatus Status = VehicleStatus.InStock);

public record UpdateVehicleRequest(
    string Make,
    string Model,
    int Year,
    string? Trim,
    string? Color,
    int Mileage,
    decimal ListPrice,
    decimal Cost,
    VehicleStatus Status,
    DateOnly AcquiredDate,
    DateOnly? SoldDate);

/// <summary>Query filter for the inventory list. All fields optional.</summary>
public record VehicleFilter(
    string? Make = null,
    string? Model = null,
    VehicleStatus? Status = null,
    int? MinAgeDays = null,
    int? MaxAgeDays = null,
    bool? AgingOnly = null,
    string? Search = null,
    string SortBy = "age",
    bool Desc = true,
    int Page = 1,
    int PageSize = 25);

public record CreateActionRequest(
    ActionType ActionType,
    ActionStatus Status,
    string? Note,
    string? CreatedBy);

public record UpdateActionRequest(
    ActionType ActionType,
    ActionStatus Status,
    string? Note);

public record UpsertStrategyRequest(
    StrategyScope Scope,
    string ScopeKey,
    string Name,
    string? Description,
    int? TargetDaysToSell,
    decimal? DiscountPercent,
    bool IsActive,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo);

/// <summary>Generic paged result wrapper for list endpoints.</summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

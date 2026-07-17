using StockLens.Domain.Enums;

namespace StockLens.Application.Dtos;

public record BusinessStrategyDto(
    Guid Id,
    StrategyScope Scope,
    string ScopeKey,
    string Name,
    string? Description,
    int? TargetDaysToSell,
    decimal? DiscountPercent,
    bool IsActive,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo);

/// <summary>The single strategy resolved as applicable to a vehicle, plus why it won.</summary>
public record EffectiveStrategyDto(
    Guid VehicleId,
    BusinessStrategyDto Strategy,
    StrategyScope MatchedScope);

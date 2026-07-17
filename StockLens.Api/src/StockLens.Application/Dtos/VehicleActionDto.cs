using StockLens.Domain.Enums;

namespace StockLens.Application.Dtos;

public record VehicleActionDto(
    Guid Id,
    Guid VehicleId,
    ActionType ActionType,
    ActionStatus Status,
    string? Note,
    string CreatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

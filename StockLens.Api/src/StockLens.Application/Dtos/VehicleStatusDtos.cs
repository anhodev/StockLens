using StockLens.Domain.Enums;

namespace StockLens.Application.Dtos;

/// <summary>
/// Moves a vehicle to <paramref name="ToStatus"/>. Which of the remaining fields are
/// required depends on the target status; see ChangeVehicleStatusRequestValidator.
/// </summary>
/// <param name="ToStatus">The status to move to.</param>
/// <param name="Reason">Why the vehicle is moving. Required for Hold and Open.</param>
/// <param name="DepositAmount">Deposit taken. Required for Deposited.</param>
/// <param name="SalePrice">Agreed price. Required for Sold.</param>
/// <param name="SoldDate">Business date of the sale. Required for Sold.</param>
/// <param name="SalespersonId">Salesperson credited. Required for Deposited and Sold.</param>
/// <param name="ChangedBy">Who performed the change; defaults to "manager".</param>
public record ChangeVehicleStatusRequest(
    VehicleStatus ToStatus,
    string? Reason = null,
    decimal? DepositAmount = null,
    decimal? SalePrice = null,
    DateOnly? SoldDate = null,
    Guid? SalespersonId = null,
    string? ChangedBy = null);

public record VehicleStatusChangeDto(
    Guid Id,
    Guid VehicleId,
    VehicleStatus FromStatus,
    VehicleStatus ToStatus,
    string? Reason,
    decimal? DepositAmount,
    decimal? SalePrice,
    string? SalespersonName,
    DateOnly EffectiveDate,
    string ChangedBy,
    DateTimeOffset CreatedAt);

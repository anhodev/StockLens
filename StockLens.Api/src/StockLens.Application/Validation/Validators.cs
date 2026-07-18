using FluentValidation;
using StockLens.Application.Dtos;
using StockLens.Domain.Enums;

namespace StockLens.Application.Validation;

public class CreateVehicleRequestValidator : AbstractValidator<CreateVehicleRequest>
{
    public CreateVehicleRequestValidator()
    {
        RuleFor(x => x.Vin).NotEmpty().Length(5, 32);
        RuleFor(x => x.Make).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Year).InclusiveBetween(1900, 2100);
        RuleFor(x => x.Mileage).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ListPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0);
    }
}

public class UpdateVehicleRequestValidator : AbstractValidator<UpdateVehicleRequest>
{
    public UpdateVehicleRequestValidator()
    {
        RuleFor(x => x.Make).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Year).InclusiveBetween(1900, 2100);
        RuleFor(x => x.Mileage).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ListPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0);
    }
}

/// <summary>
/// Enforces the evidence each status transition requires: a deposit and a salesperson to
/// take a deposit, a reason to hold or release, and a price/date/salesperson to sell.
/// </summary>
public class ChangeVehicleStatusRequestValidator : AbstractValidator<ChangeVehicleStatusRequest>
{
    public ChangeVehicleStatusRequestValidator()
    {
        RuleFor(x => x.ToStatus).IsInEnum();
        RuleFor(x => x.Reason).MaximumLength(500);

        When(x => x.ToStatus == VehicleStatus.Deposited, () =>
        {
            RuleFor(x => x.DepositAmount)
                .NotNull().WithMessage("A deposit amount is required when taking a deposit.")
                .GreaterThan(0).WithMessage("The deposit must be greater than zero.");
            RuleFor(x => x.SalespersonId)
                .NotNull().WithMessage("A salesperson is required when taking a deposit.");
        });

        When(x => x.ToStatus == VehicleStatus.Hold, () =>
        {
            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("A reason is required when placing a vehicle on hold.");
        });

        When(x => x.ToStatus == VehicleStatus.Sold, () =>
        {
            RuleFor(x => x.SoldDate)
                .NotNull().WithMessage("A sale date is required when marking a vehicle sold.");
            RuleFor(x => x.SalespersonId)
                .NotNull().WithMessage("A salesperson is required when marking a vehicle sold.");
            RuleFor(x => x.SalePrice)
                .NotNull().WithMessage("A sale price is required when marking a vehicle sold.")
                .GreaterThan(0).WithMessage("The sale price must be greater than zero.");
        });

        When(x => x.ToStatus == VehicleStatus.Open, () =>
        {
            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("A reason is required when returning a vehicle to open.");
        });
    }
}

public class CreateActionRequestValidator : AbstractValidator<CreateActionRequest>
{
    public CreateActionRequestValidator()
    {
        RuleFor(x => x.Note).MaximumLength(1000);
    }
}

public class UpdateActionRequestValidator : AbstractValidator<UpdateActionRequest>
{
    public UpdateActionRequestValidator()
    {
        RuleFor(x => x.Note).MaximumLength(1000);
    }
}

public class UpsertStrategyRequestValidator : AbstractValidator<UpsertStrategyRequest>
{
    public UpsertStrategyRequestValidator()
    {
        RuleFor(x => x.ScopeKey).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.TargetDaysToSell).GreaterThan(0).When(x => x.TargetDaysToSell.HasValue);
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100).When(x => x.DiscountPercent.HasValue);
        RuleFor(x => x.EffectiveTo)
            .GreaterThanOrEqualTo(x => x.EffectiveFrom)
            .When(x => x.EffectiveTo.HasValue)
            .WithMessage("EffectiveTo must be on or after EffectiveFrom.");
    }
}

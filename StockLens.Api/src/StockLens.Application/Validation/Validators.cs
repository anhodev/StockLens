using FluentValidation;
using StockLens.Application.Dtos;

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

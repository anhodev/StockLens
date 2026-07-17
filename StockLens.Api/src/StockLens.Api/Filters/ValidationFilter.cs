using FluentValidation;

namespace StockLens.Api.Filters;

/// <summary>
/// Minimal-API endpoint filter that runs a FluentValidation validator against the
/// first argument of type <typeparamref name="T"/> and returns 400 with the errors.
/// </summary>
public class ValidationFilter<T> : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
        if (validator is not null)
        {
            var arg = context.Arguments.OfType<T>().FirstOrDefault();
            if (arg is not null)
            {
                var result = await validator.ValidateAsync(arg);
                if (!result.IsValid)
                {
                    var errors = result.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                    return Results.ValidationProblem(errors);
                }
            }
        }

        return await next(context);
    }
}

public static class ValidationFilterExtensions
{
    public static RouteHandlerBuilder WithValidation<T>(this RouteHandlerBuilder builder) =>
        builder.AddEndpointFilter<ValidationFilter<T>>();
}

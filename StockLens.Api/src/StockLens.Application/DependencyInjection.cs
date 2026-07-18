using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using StockLens.Application.Abstractions;
using StockLens.Application.Services;
using StockLens.Application.Strategies;

namespace StockLens.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IStrategyResolver, StrategyResolver>();
        services.AddScoped<DashboardService>();
        services.AddScoped<VehicleStatusService>();
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        return services;
    }
}

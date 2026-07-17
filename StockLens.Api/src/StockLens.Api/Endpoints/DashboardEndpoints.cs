using StockLens.Application.Services;

namespace StockLens.Api.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/dashboard/summary", async (DashboardService dashboard, CancellationToken ct) =>
                Results.Ok(await dashboard.GetSummaryAsync(ct)))
            .WithTags("Dashboard");

        return app;
    }
}

using Microsoft.EntityFrameworkCore;
using StockLens.Application.Abstractions;
using StockLens.Application.Dtos;

namespace StockLens.Api.Endpoints;

public static class SalespersonEndpoints
{
    public static IEndpointRouteBuilder MapSalespersonEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/salespeople").WithTags("Salespeople");

        group.MapGet("/", GetSalespeople);
        group.MapGet("/{id:guid}", GetSalesperson);

        return app;
    }

    /// <summary>The sales team with each member's lifetime sales count and revenue.</summary>
    private static async Task<IResult> GetSalespeople(
        IApplicationDbContext db, bool? activeOnly, CancellationToken ct)
    {
        var query = db.Salespeople.AsNoTracking().AsQueryable();
        if (activeOnly == true) query = query.Where(s => s.IsActive);

        // Aggregate in the database rather than loading each person's sales.
        var list = await query
            .OrderBy(s => s.FullName)
            .Select(s => new SalespersonDto(
                s.Id, s.FullName, s.Email, s.Team, s.HireDate, s.IsActive,
                s.Sales.Count(),
                s.Sales.Sum(x => (decimal?)x.SalePrice) ?? 0m))
            .ToListAsync(ct);

        return Results.Ok(list);
    }

    private static async Task<IResult> GetSalesperson(Guid id, IApplicationDbContext db, CancellationToken ct)
    {
        var person = await db.Salespeople
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new SalespersonDto(
                s.Id, s.FullName, s.Email, s.Team, s.HireDate, s.IsActive,
                s.Sales.Count(),
                s.Sales.Sum(x => (decimal?)x.SalePrice) ?? 0m))
            .FirstOrDefaultAsync(ct);

        return person is null ? Results.NotFound() : Results.Ok(person);
    }
}

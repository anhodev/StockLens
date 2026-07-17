using Microsoft.EntityFrameworkCore;
using StockLens.Api.Filters;
using StockLens.Application.Abstractions;
using StockLens.Application.Dtos;
using StockLens.Application.Mapping;
using StockLens.Domain.Entities;

namespace StockLens.Api.Endpoints;

public static class ActionEndpoints
{
    private const string LogCategory = "StockLens.Api.Actions";

    public static IEndpointRouteBuilder MapActionEndpoints(this IEndpointRouteBuilder app)
    {
        var vehicles = app.MapGroup("/api/vehicles").WithTags("Actions");
        vehicles.MapGet("/{vehicleId:guid}/actions", GetActions);
        vehicles.MapPost("/{vehicleId:guid}/actions", CreateAction).WithValidation<CreateActionRequest>();

        var actions = app.MapGroup("/api/actions").WithTags("Actions");
        actions.MapPut("/{id:guid}", UpdateAction).WithValidation<UpdateActionRequest>();

        return app;
    }

    private static async Task<IResult> GetActions(Guid vehicleId, IApplicationDbContext db, CancellationToken ct)
    {
        if (!await db.Vehicles.AnyAsync(v => v.Id == vehicleId, ct))
            return Results.NotFound();

        var actions = await db.VehicleActions
            .AsNoTracking()
            .Where(a => a.VehicleId == vehicleId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

        return Results.Ok(actions.Select(a => a.ToDto()).ToList());
    }

    private static async Task<IResult> CreateAction(
        Guid vehicleId, CreateActionRequest req, IApplicationDbContext db,
        IInventoryNotifier notifier, ILoggerFactory loggerFactory, CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger(LogCategory);

        if (!await db.Vehicles.AnyAsync(v => v.Id == vehicleId, ct))
            return Results.NotFound();

        var action = new VehicleAction
        {
            VehicleId = vehicleId,
            ActionType = req.ActionType,
            Status = req.Status,
            Note = req.Note,
            CreatedBy = string.IsNullOrWhiteSpace(req.CreatedBy) ? "manager" : req.CreatedBy!,
        };

        db.VehicleActions.Add(action);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Action {ActionId} ({ActionType}) logged on vehicle {VehicleId} by {CreatedBy}",
            action.Id, action.ActionType, vehicleId, action.CreatedBy);

        var dto = action.ToDto();
        await notifier.ActionChangedAsync(dto, ct);

        return Results.Created($"/api/actions/{action.Id}", dto);
    }

    private static async Task<IResult> UpdateAction(
        Guid id, UpdateActionRequest req, IApplicationDbContext db,
        IInventoryNotifier notifier, ILoggerFactory loggerFactory, CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger(LogCategory);

        var action = await db.VehicleActions.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (action is null) return Results.NotFound();

        action.ActionType = req.ActionType;
        action.Status = req.Status;
        action.Note = req.Note;
        action.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Action {ActionId} updated to status {Status}", action.Id, action.Status);

        var dto = action.ToDto();
        await notifier.ActionChangedAsync(dto, ct);

        return Results.Ok(dto);
    }
}

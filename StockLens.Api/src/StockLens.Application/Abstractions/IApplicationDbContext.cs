using Microsoft.EntityFrameworkCore;
using StockLens.Domain.Entities;

namespace StockLens.Application.Abstractions;

/// <summary>
/// Application-facing view of the persistence store. Implemented by the EF Core
/// DbContext in the Infrastructure layer, keeping Application free of a concrete provider.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Vehicle> Vehicles { get; }
    DbSet<VehicleAction> VehicleActions { get; }
    DbSet<BusinessStrategy> Strategies { get; }
    DbSet<SalesRecord> Sales { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

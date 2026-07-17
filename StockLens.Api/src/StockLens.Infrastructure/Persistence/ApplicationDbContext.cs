using Microsoft.EntityFrameworkCore;
using StockLens.Application.Abstractions;
using StockLens.Domain.Entities;

namespace StockLens.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<VehicleAction> VehicleActions => Set<VehicleAction>();
    public DbSet<BusinessStrategy> Strategies => Set<BusinessStrategy>();
    public DbSet<SalesRecord> Sales => Set<SalesRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

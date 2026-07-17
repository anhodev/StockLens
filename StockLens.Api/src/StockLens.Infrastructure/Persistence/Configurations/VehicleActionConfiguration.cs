using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockLens.Domain.Entities;

namespace StockLens.Infrastructure.Persistence.Configurations;

public class VehicleActionConfiguration : IEntityTypeConfiguration<VehicleAction>
{
    public void Configure(EntityTypeBuilder<VehicleAction> builder)
    {
        builder.ToTable("vehicle_actions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ActionType).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Note).HasMaxLength(1000);
        builder.Property(x => x.CreatedBy).HasMaxLength(128).IsRequired();

        builder.HasIndex(x => x.VehicleId);
    }
}

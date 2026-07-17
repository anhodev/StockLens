using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockLens.Domain.Entities;

namespace StockLens.Infrastructure.Persistence.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("vehicles");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Vin).HasMaxLength(32).IsRequired();
        builder.HasIndex(x => x.Vin).IsUnique();

        builder.Property(x => x.Make).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Model).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Trim).HasMaxLength(64);
        builder.Property(x => x.Color).HasMaxLength(32);

        builder.Property(x => x.ListPrice).HasColumnType("numeric(12,2)");
        builder.Property(x => x.Cost).HasColumnType("numeric(12,2)");

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);

        builder.HasIndex(x => x.Make);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.AcquiredDate);

        builder.HasMany(x => x.Actions)
            .WithOne(a => a.Vehicle!)
            .HasForeignKey(a => a.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

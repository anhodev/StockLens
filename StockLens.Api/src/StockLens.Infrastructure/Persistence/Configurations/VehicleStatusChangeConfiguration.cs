using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockLens.Domain.Entities;

namespace StockLens.Infrastructure.Persistence.Configurations;

public class VehicleStatusChangeConfiguration : IEntityTypeConfiguration<VehicleStatusChange>
{
    public void Configure(EntityTypeBuilder<VehicleStatusChange> builder)
    {
        builder.ToTable("vehicle_status_changes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.DepositAmount).HasColumnType("numeric(12,2)");
        builder.Property(x => x.SalePrice).HasColumnType("numeric(12,2)");
        builder.Property(x => x.ChangedBy).HasMaxLength(128).IsRequired();

        builder.HasOne(x => x.Salesperson)
            .WithMany()
            .HasForeignKey(x => x.SalespersonId)
            // Audit rows must outlive a salesperson leaving.
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.VehicleId);
        builder.HasIndex(x => x.CreatedAt);
    }
}

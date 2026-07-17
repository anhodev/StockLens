using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockLens.Domain.Entities;

namespace StockLens.Infrastructure.Persistence.Configurations;

public class SalesRecordConfiguration : IEntityTypeConfiguration<SalesRecord>
{
    public void Configure(EntityTypeBuilder<SalesRecord> builder)
    {
        builder.ToTable("sales_records");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SalePrice).HasColumnType("numeric(12,2)");
        builder.Property(x => x.SoldBy).HasMaxLength(128).IsRequired();

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.SoldDate);
    }
}

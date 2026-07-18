using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockLens.Domain.Entities;

namespace StockLens.Infrastructure.Persistence.Configurations;

public class SalespersonConfiguration : IEntityTypeConfiguration<Salesperson>
{
    public void Configure(EntityTypeBuilder<Salesperson> builder)
    {
        builder.ToTable("salespeople");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FullName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(256);
        builder.Property(x => x.Team).HasMaxLength(32);

        builder.HasIndex(x => x.FullName);
        builder.HasIndex(x => x.IsActive);

        builder.HasMany(x => x.Sales)
            .WithOne(s => s.Salesperson!)
            .HasForeignKey(s => s.SalespersonId)
            // Sales history must survive a salesperson leaving the dealership.
            .OnDelete(DeleteBehavior.Restrict);
    }
}

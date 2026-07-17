using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockLens.Domain.Entities;

namespace StockLens.Infrastructure.Persistence.Configurations;

public class BusinessStrategyConfiguration : IEntityTypeConfiguration<BusinessStrategy>
{
    public void Configure(EntityTypeBuilder<BusinessStrategy> builder)
    {
        builder.ToTable("business_strategies");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Scope).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.ScopeKey).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.DiscountPercent).HasColumnType("numeric(5,2)");

        // Fast lookup by scope + key during strategy resolution.
        builder.HasIndex(x => new { x.Scope, x.ScopeKey });
    }
}

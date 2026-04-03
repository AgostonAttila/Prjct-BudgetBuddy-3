using BudgetBuddy.API.VSA.Common.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetBuddy.API.VSA.Common.Infrastructure.Persistence.Configurations;

public class PriceSnapshotConfiguration : IEntityTypeConfiguration<PriceSnapshot>
{
    public void Configure(EntityTypeBuilder<PriceSnapshot> builder)
    {
        builder.HasKey(p => new { p.Symbol, p.Date });

        builder.Property(p => p.Symbol)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(p => p.ClosePrice)
            .HasPrecision(18, 8);

        builder.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(p => p.Source)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(p => p.Date)
            .HasDatabaseName("IX_PriceSnapshots_Date");
    }
}

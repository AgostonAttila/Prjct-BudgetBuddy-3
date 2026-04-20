using BudgetBuddy.Module.Investments.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetBuddy.Module.Investments.Persistence.Configurations;

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

        builder.HasIndex(p => new { p.Symbol, p.Date })
            .IsDescending(false, true)
            .HasDatabaseName("IX_PriceSnapshots_Symbol_Date_Desc");
    }
}

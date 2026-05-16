using BudgetBuddy.Service.Analytics.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetBuddy.Service.Analytics.Persistence.Configurations;

public class PriceSnapshotReadModelConfiguration : IEntityTypeConfiguration<PriceSnapshotReadModel>
{
    public void Configure(EntityTypeBuilder<PriceSnapshotReadModel> builder)
    {
        builder.ToTable("price_snapshots", "analytics");
        builder.HasKey(p => new { p.Symbol, p.Date });
        builder.Property(p => p.Symbol).HasMaxLength(20).IsRequired();
        builder.Property(p => p.PriceUsd).HasPrecision(18, 8);

        builder.HasIndex(p => p.Symbol)
            .HasDatabaseName("IX_AnalyticsPriceSnapshots_Symbol");
    }
}

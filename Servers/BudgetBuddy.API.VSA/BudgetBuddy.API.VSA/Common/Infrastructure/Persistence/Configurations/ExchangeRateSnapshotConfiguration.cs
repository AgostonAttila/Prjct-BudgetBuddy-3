using BudgetBuddy.API.VSA.Common.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetBuddy.API.VSA.Common.Infrastructure.Persistence.Configurations;

public class ExchangeRateSnapshotConfiguration : IEntityTypeConfiguration<ExchangeRateSnapshot>
{
    public void Configure(EntityTypeBuilder<ExchangeRateSnapshot> builder)
    {
        builder.HasKey(e => new { e.Currency, e.Date });

        builder.Property(e => e.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(e => e.RateToUsd)
            .HasPrecision(18, 10);

        builder.HasIndex(e => e.Date)
            .HasDatabaseName("IX_ExchangeRateSnapshots_Date");
    }
}

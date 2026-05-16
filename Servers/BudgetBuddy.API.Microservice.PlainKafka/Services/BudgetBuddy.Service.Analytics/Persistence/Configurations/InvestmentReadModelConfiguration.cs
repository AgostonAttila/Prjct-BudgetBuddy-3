using BudgetBuddy.Service.Analytics.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetBuddy.Service.Analytics.Persistence.Configurations;

public class InvestmentReadModelConfiguration : IEntityTypeConfiguration<InvestmentReadModel>
{
    public void Configure(EntityTypeBuilder<InvestmentReadModel> builder)
    {
        builder.ToTable("investments", "analytics");
        builder.HasKey(i => i.InvestmentId);
        builder.Property(i => i.UserId).HasMaxLength(256).IsRequired();
        builder.Property(i => i.Symbol).HasMaxLength(20).IsRequired();
        builder.Property(i => i.Name).HasMaxLength(256).IsRequired();
        builder.Property(i => i.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(i => i.Quantity).HasPrecision(18, 8);
        builder.Property(i => i.PurchasePrice).HasPrecision(18, 8);

        builder.HasIndex(i => i.UserId)
            .HasDatabaseName("IX_AnalyticsInvestments_UserId");
        builder.HasIndex(i => new { i.UserId, i.PurchaseDate })
            .HasDatabaseName("IX_AnalyticsInvestments_UserId_PurchaseDate");
    }
}

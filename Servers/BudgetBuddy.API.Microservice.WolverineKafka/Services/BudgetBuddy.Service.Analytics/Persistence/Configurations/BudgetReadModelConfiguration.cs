using BudgetBuddy.Service.Analytics.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetBuddy.Service.Analytics.Persistence.Configurations;

public class BudgetReadModelConfiguration : IEntityTypeConfiguration<BudgetReadModel>
{
    public void Configure(EntityTypeBuilder<BudgetReadModel> builder)
    {
        builder.ToTable("budgets", "analytics");
        builder.HasKey(b => b.BudgetId);
        builder.Property(b => b.UserId).HasMaxLength(256).IsRequired();
        builder.Property(b => b.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(b => b.Amount).HasPrecision(18, 2);

        builder.HasIndex(b => new { b.UserId, b.Year, b.Month })
            .HasDatabaseName("IX_AnalyticsBudgets_UserId_Year_Month");
    }
}

using BudgetBuddy.Service.Analytics.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetBuddy.Service.Analytics.Persistence.Configurations;

public class TransactionReadModelConfiguration : IEntityTypeConfiguration<TransactionReadModel>
{
    public void Configure(EntityTypeBuilder<TransactionReadModel> builder)
    {
        builder.ToTable("transactions", "analytics");
        builder.HasKey(t => t.TransactionId);
        builder.Property(t => t.UserId).HasMaxLength(256).IsRequired();
        builder.Property(t => t.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(t => t.Amount).HasPrecision(18, 2);

        builder.HasIndex(t => t.UserId)
            .HasDatabaseName("IX_AnalyticsTransactions_UserId");
        builder.HasIndex(t => new { t.UserId, t.TransactionDate })
            .HasDatabaseName("IX_AnalyticsTransactions_UserId_Date");
        builder.HasIndex(t => new { t.UserId, t.TransactionType, t.TransactionDate })
            .HasDatabaseName("IX_AnalyticsTransactions_UserId_Type_Date");
        builder.HasIndex(t => t.AccountId)
            .HasDatabaseName("IX_AnalyticsTransactions_AccountId");
    }
}

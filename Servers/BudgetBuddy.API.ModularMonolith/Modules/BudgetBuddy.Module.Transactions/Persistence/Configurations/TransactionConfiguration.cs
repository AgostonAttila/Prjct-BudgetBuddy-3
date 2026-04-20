using BudgetBuddy.Module.Transactions.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetBuddy.Module.Transactions.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Amount)
            .HasPrecision(18, 2);

        builder.Property(t => t.RefCurrencyAmount)
            .HasPrecision(18, 2);

        builder.Property(t => t.CurrencyCode)
            .IsRequired()
            .HasMaxLength(3);

        // Cross-module FKs: AccountId, CategoryId, TypeId
        // No EF navigation — FK constraints maintained at DB level from migrations
        builder.Property(t => t.AccountId).IsRequired();
        builder.Property(t => t.CategoryId).IsRequired(false);
        builder.Property(t => t.TypeId).IsRequired(false);

        builder.HasIndex(t => t.TransactionDate)
            .HasDatabaseName("IX_Transactions_TransactionDate");

        builder.HasIndex(t => t.UserId)
            .HasDatabaseName("IX_Transactions_UserId");

        builder.HasIndex(t => t.AccountId)
            .HasDatabaseName("IX_Transactions_AccountId");

        builder.HasIndex(t => t.CategoryId)
            .HasDatabaseName("IX_Transactions_CategoryId");

        builder.HasIndex(t => t.TypeId)
            .HasDatabaseName("IX_Transactions_TypeId");

        builder.HasIndex(t => new { t.UserId, t.TransactionDate, t.TransactionType, t.CategoryId })
            .HasDatabaseName("IX_Transactions_BudgetAlert_Optimized");

        builder.HasIndex(t => new { t.UserId, t.AccountId, t.TransactionDate })
            .HasDatabaseName("IX_Transactions_Dashboard_Optimized");
    }
}

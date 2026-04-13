using BudgetBuddy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetBuddy.Infrastructure.Persistence.Configurations;

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

        builder.HasOne(t => t.Account)
            .WithMany(a => a.Transactions)
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Category)
            .WithMany(c => c.Transactions)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.Type)
            .WithMany(ct => ct.Transactions)
            .HasForeignKey(t => t.TypeId)
            .OnDelete(DeleteBehavior.SetNull);

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

        // Composite index for BudgetAlert queries (GetCategorySpendingForMonthAsync)
        builder.HasIndex(t => new { t.UserId, t.TransactionDate, t.TransactionType, t.CategoryId })
            .HasDatabaseName("IX_Transactions_BudgetAlert_Optimized");

        // Composite index for Dashboard/Report queries
        builder.HasIndex(t => new { t.UserId, t.AccountId, t.TransactionDate })
            .HasDatabaseName("IX_Transactions_Dashboard_Optimized");
    }
}

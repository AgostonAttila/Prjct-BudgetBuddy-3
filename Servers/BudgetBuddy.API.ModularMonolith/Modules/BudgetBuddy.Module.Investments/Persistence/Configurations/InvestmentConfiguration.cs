using BudgetBuddy.Module.Investments.Domain;
using BudgetBuddy.Shared.Kernel.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetBuddy.Module.Investments.Persistence.Configurations;

public class InvestmentConfiguration : IEntityTypeConfiguration<Investment>
{
    public void Configure(EntityTypeBuilder<Investment> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Symbol)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(i => i.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.Quantity)
            .HasPrecision(18, 8);

        builder.Property(i => i.PurchasePrice)
            .HasPrecision(18, 8);

        builder.Property(i => i.CurrencyCode)
            .IsRequired()
            .HasMaxLength(3);

        // FK to User (public schema)
        builder.HasOne(i => i.User)
            .WithMany()
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // AccountId is a cross-module FK (accounts schema) — no EF navigation
        builder.Property(i => i.AccountId)
            .IsRequired(false);

        builder.HasIndex(i => new { i.UserId, i.Symbol });
        builder.HasIndex(i => i.PurchaseDate);

        builder.HasIndex(i => i.AccountId)
            .HasDatabaseName("IX_Investments_AccountId");

        builder.HasIndex(i => new { i.UserId, i.Symbol, i.PurchaseDate, i.Quantity, i.PurchasePrice })
            .IsUnique()
            .HasDatabaseName("IX_Investments_Dedup");

        // Active investment filter: WHERE UserId = ? AND SoldDate IS NULL
        builder.HasIndex(i => new { i.UserId, i.SoldDate })
            .HasDatabaseName("IX_Investments_UserId_SoldDate");
    }
}

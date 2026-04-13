using BudgetBuddy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetBuddy.Infrastructure.Persistence.Configurations;

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
            .HasPrecision(18, 8); // Support crypto decimals

        builder.Property(i => i.PurchasePrice)
            .HasPrecision(18, 8);

        builder.Property(i => i.CurrencyCode)
            .IsRequired()
            .HasMaxLength(3);

        builder.HasOne(i => i.User)
            .WithMany(u => u.Investments)
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Account)
            .WithMany(a => a.Investments)
            .HasForeignKey(i => i.AccountId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(i => new { i.UserId, i.Symbol });
        builder.HasIndex(i => i.PurchaseDate);

        builder.HasIndex(i => i.AccountId)
            .HasDatabaseName("IX_Investments_AccountId");

        // Prevent duplicate purchases: same symbol, date, quantity, and price
        builder.HasIndex(i => new { i.UserId, i.Symbol, i.PurchaseDate, i.Quantity, i.PurchasePrice })
            .IsUnique()
            .HasDatabaseName("IX_Investments_Dedup");
    }
}

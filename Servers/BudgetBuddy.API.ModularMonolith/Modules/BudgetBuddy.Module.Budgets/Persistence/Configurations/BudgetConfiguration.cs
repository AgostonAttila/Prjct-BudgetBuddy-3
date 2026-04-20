using BudgetBuddy.Module.Budgets.Domain;
using BudgetBuddy.Shared.Kernel.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetBuddy.Module.Budgets.Persistence.Configurations;

public class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.Amount)
            .HasPrecision(18, 2);

        builder.Property(b => b.CurrencyCode)
            .IsRequired()
            .HasMaxLength(3);

        // FK to User (public schema)
        builder.HasOne(b => b.User)
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // CategoryId is a cross-module FK (referencedata schema) — no EF navigation
        builder.Property(b => b.CategoryId).IsRequired();

        builder.HasIndex(b => new { b.UserId, b.CategoryId, b.Year, b.Month })
            .IsUnique()
            .HasDatabaseName("IX_Budgets_Unique");

        builder.HasIndex(b => new { b.UserId, b.Year, b.Month })
            .HasDatabaseName("IX_Budgets_UserYearMonth");
    }
}

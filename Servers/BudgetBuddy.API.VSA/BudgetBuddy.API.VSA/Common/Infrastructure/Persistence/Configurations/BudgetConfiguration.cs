using BudgetBuddy.API.VSA.Common.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetBuddy.API.VSA.Common.Infrastructure.Persistence.Configurations;

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

        builder.HasOne(b => b.User)
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.Category)
            .WithMany()
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(b => new { b.UserId, b.CategoryId, b.Year, b.Month })
            .IsUnique() // One budget per category per month
            .HasDatabaseName("IX_Budgets_Unique");

        // Additional index for GetBudgetsForMonthAsync queries
        builder.HasIndex(b => new { b.UserId, b.Year, b.Month })
            .HasDatabaseName("IX_Budgets_UserYearMonth");
    }
}

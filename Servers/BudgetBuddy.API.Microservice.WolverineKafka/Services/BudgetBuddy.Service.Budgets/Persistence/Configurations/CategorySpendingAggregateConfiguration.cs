using BudgetBuddy.Service.Budgets.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetBuddy.Service.Budgets.Persistence.Configurations;

public class CategorySpendingAggregateConfiguration : IEntityTypeConfiguration<CategorySpendingAggregate>
{
    public void Configure(EntityTypeBuilder<CategorySpendingAggregate> builder)
    {
        builder.ToTable("category_spending_aggregates", "budgets");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.UserId).HasMaxLength(256).IsRequired();
        builder.Property(a => a.CurrencyCode).HasMaxLength(10).IsRequired();
        builder.Property(a => a.TotalExpense).HasPrecision(18, 4);
        builder.HasIndex(a => new { a.UserId, a.CategoryId, a.Year, a.Month, a.CurrencyCode }).IsUnique();
        builder.HasIndex(a => new { a.UserId, a.Year, a.Month });
    }
}

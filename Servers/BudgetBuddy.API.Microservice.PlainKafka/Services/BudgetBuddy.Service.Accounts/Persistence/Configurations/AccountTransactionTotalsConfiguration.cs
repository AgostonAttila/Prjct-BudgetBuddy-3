using BudgetBuddy.Service.Accounts.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetBuddy.Service.Accounts.Persistence.Configurations;

public class AccountTransactionTotalsConfiguration : IEntityTypeConfiguration<AccountTransactionTotals>
{
    public void Configure(EntityTypeBuilder<AccountTransactionTotals> builder)
    {
        builder.ToTable("account_transaction_totals", "accounts");
        builder.HasKey(t => t.AccountId);
        builder.Property(t => t.TotalIncome).HasPrecision(18, 4);
        builder.Property(t => t.TotalExpense).HasPrecision(18, 4);
        builder.Property(t => t.LastProcessedMessageId).IsRequired(false);
    }
}

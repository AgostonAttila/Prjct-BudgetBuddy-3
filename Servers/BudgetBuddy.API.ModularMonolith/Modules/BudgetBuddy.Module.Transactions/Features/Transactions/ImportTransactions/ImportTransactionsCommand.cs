using BudgetBuddy.Shared.Kernel.Constants;
using BudgetBuddy.Module.Transactions.Features.Transactions.Services;

namespace BudgetBuddy.Module.Transactions.Features.ImportTransactions;

public record ImportTransactionsCommand(
    Stream FileStream
) : IRequest<ImportResult>, ICacheInvalidator
{
    public string[] CacheTags => [Tags.Transactions, Tags.AccountBalance, Tags.PortfolioValue, Tags.Dashboard, Tags.MonthlySummary, Tags.IncomeVsExpense, Tags.SpendingByCategory, Tags.BudgetVsActual, Tags.BudgetAlerts];
}

using BudgetBuddy.Service.Transactions.Features.Transactions.Services;
using BudgetBuddy.Shared.Kernel.Constants;

namespace BudgetBuddy.Service.Transactions.Features.ImportTransactions;

public record ImportTransactionsCommand(
    [property: SensitiveData] Stream FileStream
) : IRequest<ImportResult>, ICacheInvalidator
{
    public string[] CacheTags => [Tags.Transactions, Tags.AccountBalance, Tags.PortfolioValue, Tags.Dashboard, Tags.MonthlySummary, Tags.IncomeVsExpense, Tags.SpendingByCategory, Tags.BudgetVsActual, Tags.BudgetAlerts];
}

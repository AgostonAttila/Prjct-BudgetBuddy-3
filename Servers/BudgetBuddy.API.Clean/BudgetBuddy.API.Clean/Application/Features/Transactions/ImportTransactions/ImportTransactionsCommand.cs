using BudgetBuddy.Application.Common.Contracts;
using BudgetBuddy.Domain.Constants;
using BudgetBuddy.Application.Common.Contracts;

namespace BudgetBuddy.Application.Features.Transactions.ImportTransactions;

public record ImportTransactionsCommand(
    Stream FileStream
) : IRequest<ImportResult>, ICacheInvalidator
{
    public string[] CacheTags => [Tags.Transactions, Tags.AccountBalance, Tags.PortfolioValue, Tags.Dashboard, Tags.MonthlySummary, Tags.IncomeVsExpense, Tags.SpendingByCategory, Tags.BudgetVsActual, Tags.BudgetAlerts];
}

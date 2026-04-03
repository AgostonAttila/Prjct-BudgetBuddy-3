using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Common.Domain.Constants;
using BudgetBuddy.API.VSA.Common.Infrastructure.DataExchange;

namespace BudgetBuddy.API.VSA.Features.Transactions.ImportTransactions;

public record ImportTransactionsCommand(
    Stream FileStream
) : IRequest<ImportResult>, ICacheInvalidator
{
    public string[] CacheTags => [Tags.Transactions, Tags.AccountBalance, Tags.PortfolioValue, Tags.Dashboard, Tags.MonthlySummary, Tags.IncomeVsExpense, Tags.SpendingByCategory, Tags.BudgetVsActual, Tags.BudgetAlerts];
}

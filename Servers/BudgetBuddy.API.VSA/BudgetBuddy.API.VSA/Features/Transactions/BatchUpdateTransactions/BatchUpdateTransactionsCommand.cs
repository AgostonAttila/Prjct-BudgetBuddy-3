using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Common.Domain.Constants;

namespace BudgetBuddy.API.VSA.Features.Transactions.BatchUpdateTransactions;

public record BatchUpdateTransactionsCommand(
    List<Guid> TransactionIds,
    Guid? CategoryId,
    string? Labels
) : IRequest<BatchUpdateTransactionsResponse>, ICacheInvalidator
{
    public string[] CacheTags => [Tags.Transactions, Tags.AccountBalance, Tags.PortfolioValue, Tags.Dashboard, Tags.MonthlySummary, Tags.IncomeVsExpense, Tags.SpendingByCategory, Tags.BudgetVsActual, Tags.BudgetAlerts];
}

public record BatchUpdateTransactionsResponse(
    int TotalRequested,
    int SuccessCount,
    int FailedCount,
    List<string> Errors
);

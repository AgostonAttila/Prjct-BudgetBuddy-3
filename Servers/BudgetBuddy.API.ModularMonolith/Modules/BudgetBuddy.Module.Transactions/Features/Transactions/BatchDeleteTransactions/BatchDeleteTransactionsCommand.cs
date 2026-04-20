using BudgetBuddy.Shared.Kernel.Constants;

namespace BudgetBuddy.Module.Transactions.Features.BatchDeleteTransactions;

public record BatchDeleteTransactionsCommand(List<Guid> TransactionIds) : IRequest<BatchDeleteTransactionsResponse>, ICacheInvalidator
{
    public string[] CacheTags => [Tags.Transactions, Tags.AccountBalance, Tags.PortfolioValue, Tags.Dashboard, Tags.MonthlySummary, Tags.IncomeVsExpense, Tags.SpendingByCategory, Tags.BudgetVsActual, Tags.BudgetAlerts];
}

public record BatchDeleteTransactionsResponse(
    int TotalRequested,
    int SuccessCount,
    int FailedCount,
    List<string> Errors
);

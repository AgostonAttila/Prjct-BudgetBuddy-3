using BudgetBuddy.Shared.Kernel.Constants;

namespace BudgetBuddy.Module.Transactions.Features.DeleteTransaction;

public record DeleteTransactionCommand(
    Guid Id
) : IRequest<Unit>, ICacheInvalidator
{
    public string[] CacheTags => [Tags.Transactions, Tags.AccountBalance, Tags.PortfolioValue, Tags.Dashboard, Tags.MonthlySummary, Tags.IncomeVsExpense, Tags.SpendingByCategory, Tags.BudgetVsActual, Tags.BudgetAlerts];
}

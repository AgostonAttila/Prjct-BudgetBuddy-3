using BudgetBuddy.Application.Common.Contracts;
using BudgetBuddy.Domain.Constants;

namespace BudgetBuddy.Application.Features.Transactions.DeleteTransaction;

public record DeleteTransactionCommand(
    Guid Id
) : IRequest<Unit>, ICacheInvalidator
{
    public string[] CacheTags => [Tags.Transactions, Tags.AccountBalance, Tags.PortfolioValue, Tags.Dashboard, Tags.MonthlySummary, Tags.IncomeVsExpense, Tags.SpendingByCategory, Tags.BudgetVsActual, Tags.BudgetAlerts];
}

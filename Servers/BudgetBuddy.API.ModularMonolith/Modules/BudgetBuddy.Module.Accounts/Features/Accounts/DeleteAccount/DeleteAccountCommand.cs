namespace BudgetBuddy.Module.Accounts.Features.DeleteAccount;

public record DeleteAccountCommand(
    Guid Id
) : IRequest<Unit>, ICacheInvalidator
{
    public string[] CacheTags => [Tags.AccountsList, Tags.AccountBalance, Tags.PortfolioValue, Tags.Transactions, Tags.Dashboard, Tags.MonthlySummary, Tags.IncomeVsExpense, Tags.SpendingByCategory];
}

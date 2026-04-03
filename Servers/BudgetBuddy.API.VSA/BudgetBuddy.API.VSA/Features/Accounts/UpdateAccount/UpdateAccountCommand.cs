namespace BudgetBuddy.API.VSA.Features.Accounts.UpdateAccount;

public record UpdateAccountCommand(
    Guid Id,
    string Name,
    string Description,
    string DefaultCurrencyCode,
    decimal InitialBalance
) : IRequest<UpdateAccountResponse>, ICacheInvalidator
{
    public string[] CacheTags => [Tags.AccountsList, Tags.AccountBalance, Tags.PortfolioValue, Tags.Dashboard];
}

public record UpdateAccountResponse(
    Guid Id,
    string Name,
    string Description,
    string DefaultCurrencyCode,
    decimal InitialBalance
);

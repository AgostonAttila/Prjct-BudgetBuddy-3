using BudgetBuddy.API.VSA.Common.Domain.Constants;

namespace BudgetBuddy.API.VSA.Features.Accounts.CreateAccount;

public record CreateAccountCommand(
    string Name,
    string Description,
    string DefaultCurrencyCode,
    decimal InitialBalance
) : IRequest<CreateAccountResponse>, ICacheInvalidator
{
    public string[] CacheTags => [Tags.AccountsList, Tags.PortfolioValue, Tags.Dashboard];
}

public record CreateAccountResponse(
    Guid Id,
    string Name,
    string Description,
    string DefaultCurrencyCode,
    decimal InitialBalance
);



using BudgetBuddy.Domain.Constants;

namespace BudgetBuddy.Application.Features.Accounts.CreateAccount;

public record CreateAccountCommand(
    string Name,
    string Description,
    string DefaultCurrencyCode,
    decimal InitialBalance
) : IRequest<CreateAccountResponse>, ICacheInvalidator
{
    public string[] CacheTags => [Tags.AccountsList, Tags.PortfolioValue, Tags.Dashboard];
}

public abstract record CreateAccountResponse(
    Guid Id,
    string Name,
    string Description,
    string DefaultCurrencyCode,
    decimal InitialBalance
);



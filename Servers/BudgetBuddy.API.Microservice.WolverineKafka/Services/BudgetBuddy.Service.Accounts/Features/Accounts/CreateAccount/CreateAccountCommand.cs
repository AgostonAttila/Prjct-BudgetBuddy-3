using BudgetBuddy.Shared.Kernel.Constants;

namespace BudgetBuddy.Service.Accounts.Features.CreateAccount;

public record CreateAccountCommand(
    [property: SensitiveData(Strategy = MaskingStrategy.Partial)] string Name,
    [property: SensitiveData(Strategy = MaskingStrategy.Partial)] string Description,
    string DefaultCurrencyCode,
    [property: SensitiveData] decimal InitialBalance
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



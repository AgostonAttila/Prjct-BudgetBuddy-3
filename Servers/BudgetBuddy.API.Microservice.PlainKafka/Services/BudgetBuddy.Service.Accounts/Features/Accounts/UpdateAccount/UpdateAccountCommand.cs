namespace BudgetBuddy.Service.Accounts.Features.UpdateAccount;

public record UpdateAccountCommand(
    Guid Id,
    [property: SensitiveData(Strategy = MaskingStrategy.Partial)] string Name,
    [property: SensitiveData(Strategy = MaskingStrategy.Partial)] string Description,
    string DefaultCurrencyCode,
    [property: SensitiveData] decimal InitialBalance
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

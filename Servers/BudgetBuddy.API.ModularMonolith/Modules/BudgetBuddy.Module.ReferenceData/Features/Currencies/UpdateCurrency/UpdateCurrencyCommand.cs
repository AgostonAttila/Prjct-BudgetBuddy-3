using BudgetBuddy.Shared.Kernel.Constants;

namespace BudgetBuddy.Module.ReferenceData.Features.Currencies.UpdateCurrency;

public record UpdateCurrencyCommand(
    Guid Id,
    string Code,
    string Symbol,
    string Name
) : IRequest<CurrencyResponse>, ICacheInvalidator
{
    public string[] CacheTags => [Tags.Currencies];
}

public record CurrencyResponse(
    Guid Id,
    string Code,
    string Symbol,
    string Name
);

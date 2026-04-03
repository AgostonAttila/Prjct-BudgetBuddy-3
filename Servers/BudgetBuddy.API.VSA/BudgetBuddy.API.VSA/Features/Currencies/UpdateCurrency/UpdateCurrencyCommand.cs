using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Common.Domain.Constants;

namespace BudgetBuddy.API.VSA.Features.Currencies.UpdateCurrency;

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

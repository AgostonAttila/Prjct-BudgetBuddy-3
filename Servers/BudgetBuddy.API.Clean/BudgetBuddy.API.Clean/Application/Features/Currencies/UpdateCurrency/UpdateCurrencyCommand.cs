using BudgetBuddy.Application.Common.Contracts;
using BudgetBuddy.Domain.Constants;

namespace BudgetBuddy.Application.Features.Currencies.UpdateCurrency;

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

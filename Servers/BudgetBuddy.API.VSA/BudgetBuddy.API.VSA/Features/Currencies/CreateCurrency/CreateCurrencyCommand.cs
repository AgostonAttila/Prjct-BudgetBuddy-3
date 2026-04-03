using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Common.Domain.Constants;

namespace BudgetBuddy.API.VSA.Features.Currencies.CreateCurrency;

public record CreateCurrencyCommand(
    string Code,
    string Symbol,
    string Name
) : IRequest<CreateCurrencyResponse>, ICacheInvalidator
{
    public string[] CacheTags => [Tags.Currencies];
}

public record CreateCurrencyResponse(
    Guid Id,
    string Code,
    string Symbol,
    string Name
);

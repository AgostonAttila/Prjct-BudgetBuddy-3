using BudgetBuddy.Application.Common.Contracts;
using BudgetBuddy.Domain.Constants;

namespace BudgetBuddy.Application.Features.Currencies.CreateCurrency;

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

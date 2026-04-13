using BudgetBuddy.Application.Common.Contracts;
using BudgetBuddy.Domain.Constants;

namespace BudgetBuddy.Application.Features.Currencies.DeleteCurrency;

public record DeleteCurrencyCommand(
    Guid Id
) : IRequest<Unit>, ICacheInvalidator
{
    public string[] CacheTags => [Tags.Currencies, Tags.AccountsList, Tags.Transactions];
}

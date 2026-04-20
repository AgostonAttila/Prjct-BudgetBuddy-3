using BudgetBuddy.Shared.Kernel.Constants;

namespace BudgetBuddy.Module.ReferenceData.Features.Currencies.DeleteCurrency;

public record DeleteCurrencyCommand(
    Guid Id
) : IRequest<Unit>, ICacheInvalidator
{
    public string[] CacheTags => [Tags.Currencies, Tags.AccountsList, Tags.Transactions];
}

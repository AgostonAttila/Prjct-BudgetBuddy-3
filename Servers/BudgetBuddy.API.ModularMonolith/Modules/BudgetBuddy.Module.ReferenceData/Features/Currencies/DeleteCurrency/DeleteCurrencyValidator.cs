using FluentValidation;

namespace BudgetBuddy.Module.ReferenceData.Features.Currencies.DeleteCurrency;

public class DeleteCurrencyValidator : AbstractValidator<DeleteCurrencyCommand>
{
    public DeleteCurrencyValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Currency ID is required");
    }
}

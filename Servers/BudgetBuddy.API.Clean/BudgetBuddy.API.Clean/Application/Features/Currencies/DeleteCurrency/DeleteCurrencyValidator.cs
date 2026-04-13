using FluentValidation;

namespace BudgetBuddy.Application.Features.Currencies.DeleteCurrency;

public class DeleteCurrencyValidator : AbstractValidator<DeleteCurrencyCommand>
{
    public DeleteCurrencyValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Currency ID is required");
    }
}

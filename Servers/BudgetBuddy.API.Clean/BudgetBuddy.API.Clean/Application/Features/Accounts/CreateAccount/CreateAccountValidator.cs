using FluentValidation;

namespace BudgetBuddy.Application.Features.Accounts.CreateAccount;

public class CreateAccountValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("Account name is required and must be less than 200 characters");

        RuleFor(x => x.DefaultCurrencyCode)
            .NotEmpty()
            .Length(3)
            .WithMessage("Currency code must be exactly 3 characters (e.g., USD, EUR, HUF)");
    }
}

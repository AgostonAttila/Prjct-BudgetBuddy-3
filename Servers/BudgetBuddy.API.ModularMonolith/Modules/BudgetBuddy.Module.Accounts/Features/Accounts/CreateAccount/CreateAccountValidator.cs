using FluentValidation;

namespace BudgetBuddy.Module.Accounts.Features.CreateAccount;

public class CreateAccountValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("Account name is required and must be less than 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage("Description must be 500 characters or less");

        RuleFor(x => x.DefaultCurrencyCode)
            .NotEmpty()
            .Length(3)
            .Matches(@"^[A-Z]{3}$")
            .WithMessage("Currency code must be exactly 3 uppercase letters (e.g., USD, EUR, HUF)");
    }
}

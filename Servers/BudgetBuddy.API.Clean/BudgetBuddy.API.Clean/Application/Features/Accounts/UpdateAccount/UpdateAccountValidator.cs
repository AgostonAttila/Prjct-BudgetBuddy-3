using FluentValidation;

namespace BudgetBuddy.Application.Features.Accounts.UpdateAccount;

public class UpdateAccountValidator : AbstractValidator<UpdateAccountCommand>
{
    public UpdateAccountValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Account ID is required");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("Account name is required and must be less than 200 characters");

        RuleFor(x => x.DefaultCurrencyCode)
            .NotEmpty()
            .Length(3)
            .WithMessage("Currency code must be exactly 3 characters");
    }
}

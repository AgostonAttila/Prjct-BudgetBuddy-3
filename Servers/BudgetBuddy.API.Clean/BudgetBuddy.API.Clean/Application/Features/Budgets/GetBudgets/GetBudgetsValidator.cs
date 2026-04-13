using FluentValidation;

namespace BudgetBuddy.Application.Features.Budgets.GetBudgets;

public class GetBudgetsValidator : AbstractValidator<GetBudgetsQuery>
{
    public GetBudgetsValidator()
    {
        RuleFor(x => x.Year)
            .InclusiveBetween(1900, 2100)
            .When(x => x.Year.HasValue)
            .WithMessage("Year must be between 1900 and 2100");

        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12)
            .When(x => x.Month.HasValue)
            .WithMessage("Month must be between 1 and 12");
    }
}

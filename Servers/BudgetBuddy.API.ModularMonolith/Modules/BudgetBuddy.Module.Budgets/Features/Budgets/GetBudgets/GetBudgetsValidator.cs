using FluentValidation;

namespace BudgetBuddy.Module.Budgets.Features.GetBudgets;

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

        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 200)
            .WithMessage("Page size must be between 1 and 200");
    }
}

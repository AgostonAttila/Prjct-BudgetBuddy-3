using FluentValidation;

namespace BudgetBuddy.Module.Budgets.Features.GetBudgetVsActual;

public class GetBudgetVsActualValidator : AbstractValidator<GetBudgetVsActualQuery>
{
    public GetBudgetVsActualValidator()
    {
        RuleFor(x => x.Year)
            .InclusiveBetween(1900, 2100)
            .WithMessage("Year must be between 1900 and 2100");

        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12)
            .WithMessage("Month must be between 1 and 12");
    }
}

using FluentValidation;

namespace BudgetBuddy.Application.Features.Reports.GetIncomeVsExpense;

public class GetIncomeVsExpenseValidator : AbstractValidator<GetIncomeVsExpenseQuery>
{
    public GetIncomeVsExpenseValidator()
    {
        // Date range validation
        RuleFor(x => x)
            .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || x.StartDate <= x.EndDate)
            .WithMessage("Start date must be before or equal to end date")
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue);

        // Future date validation
        RuleFor(x => x.StartDate)
            .LessThanOrEqualTo(SystemClock.Instance.GetCurrentInstant().InUtc().Date)
            .When(x => x.StartDate.HasValue)
            .WithMessage("Start date cannot be in the future");

        RuleFor(x => x.EndDate)
            .LessThanOrEqualTo(SystemClock.Instance.GetCurrentInstant().InUtc().Date)
            .When(x => x.EndDate.HasValue)
            .WithMessage("End date cannot be in the future");
    }
}

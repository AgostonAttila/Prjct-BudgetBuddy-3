using FluentValidation;

namespace BudgetBuddy.Module.Analytics.Features.Reports.GetMonthlySummary;

public class GetMonthlySummaryValidator : AbstractValidator<GetMonthlySummaryQuery>
{
    public GetMonthlySummaryValidator()
    {
        RuleFor(x => x.Year)
            .InclusiveBetween(1900, 2100)
            .WithMessage("Year must be between 1900 and 2100");

        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12)
            .WithMessage("Month must be between 1 and 12");
    }
}

using FluentValidation;

namespace BudgetBuddy.API.VSA.Features.BudgetAlerts.GetBudgetAlerts;

public class GetBudgetAlertsValidator : AbstractValidator<GetBudgetAlertsQuery>
{
    public GetBudgetAlertsValidator()
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

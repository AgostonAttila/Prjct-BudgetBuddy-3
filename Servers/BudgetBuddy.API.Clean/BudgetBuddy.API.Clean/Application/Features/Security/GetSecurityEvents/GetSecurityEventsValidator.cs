using FluentValidation;

namespace BudgetBuddy.Application.Features.Security.GetSecurityEvents;

public class GetSecurityEventsValidator : AbstractValidator<GetSecurityEventsQuery>
{
    public GetSecurityEventsValidator()
    {
        // Pagination validation - DoS prevention
        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 200)
            .WithMessage("Page size must be between 1 and 200");
    }
}

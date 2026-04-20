using FluentValidation;

namespace BudgetBuddy.Module.Auth.Features.Security.GetSecurityEvents;

public class GetSecurityEventsValidator : AbstractValidator<GetSecurityEventsQuery>
{
    public GetSecurityEventsValidator()
    {
        // Pagination validation - DoS prevention
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 200)
            .WithMessage("Page size must be between 1 and 200");
    }
}

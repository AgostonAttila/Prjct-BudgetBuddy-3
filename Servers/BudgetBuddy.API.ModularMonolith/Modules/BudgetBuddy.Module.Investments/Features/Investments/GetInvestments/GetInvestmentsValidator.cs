using FluentValidation;

namespace BudgetBuddy.Module.Investments.Features.GetInvestments;

public class GetInvestmentsValidator : AbstractValidator<GetInvestmentsQuery>
{
    public GetInvestmentsValidator()
    {
        // Search term validation - prevent cache poisoning
        RuleFor(x => x.SearchTerm)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.SearchTerm))
            .WithMessage("Search term must be 100 characters or less");

        RuleFor(x => x.SearchTerm)
            .Matches(@"^[a-zA-Z0-9\s\-_.@]*$")
            .When(x => !string.IsNullOrEmpty(x.SearchTerm))
            .WithMessage("Search term contains invalid characters. Only alphanumeric, spaces, hyphens, underscores, periods, and @ are allowed.");

        // Pagination validation
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100");
    }
}

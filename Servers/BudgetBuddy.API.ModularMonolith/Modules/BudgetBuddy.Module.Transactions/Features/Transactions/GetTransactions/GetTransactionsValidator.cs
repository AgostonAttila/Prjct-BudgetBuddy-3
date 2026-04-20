using FluentValidation;

namespace BudgetBuddy.Module.Transactions.Features.GetTransactions;

public class GetTransactionsValidator : AbstractValidator<GetTransactionsQuery>
{
    private const int MinYear = 1950;
    private const int MaxYear = 2100;
    private const int MaxRangeDays = 1825; // ~5 years

    public GetTransactionsValidator()
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

        // Date range validation
        RuleFor(x => x.StartDate)
            .Must(d => d!.Value.Year >= MinYear && d.Value.Year <= MaxYear)
            .When(x => x.StartDate.HasValue)
            .WithMessage($"Start date year must be between {MinYear} and {MaxYear}");

        RuleFor(x => x.EndDate)
            .Must(d => d!.Value.Year >= MinYear && d.Value.Year <= MaxYear)
            .When(x => x.EndDate.HasValue)
            .WithMessage($"End date year must be between {MinYear} and {MaxYear}");

        RuleFor(x => x)
            .Must(x => x.EndDate!.Value >= x.StartDate!.Value)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("End date must be greater than or equal to start date");

        RuleFor(x => x)
            .Must(x => x.StartDate!.Value.PlusDays(MaxRangeDays) >= x.EndDate!.Value)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage($"Date range cannot exceed {MaxRangeDays / 365} years");

        // Pagination validation
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100");
    }
}

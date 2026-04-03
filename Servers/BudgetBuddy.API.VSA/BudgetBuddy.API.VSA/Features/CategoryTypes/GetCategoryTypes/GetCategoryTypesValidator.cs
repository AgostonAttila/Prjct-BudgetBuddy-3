using FluentValidation;

namespace BudgetBuddy.API.VSA.Features.CategoryTypes.GetCategoryTypes;

public class GetCategoryTypesValidator : AbstractValidator<GetCategoryTypesQuery>
{
    public GetCategoryTypesValidator()
    {
        // Pagination validation
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100");
    }
}

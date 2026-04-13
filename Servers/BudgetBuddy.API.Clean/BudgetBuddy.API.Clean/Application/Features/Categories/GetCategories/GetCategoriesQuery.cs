namespace BudgetBuddy.Application.Features.Categories.GetCategories;

public record GetCategoriesQuery() : IRequest<List<CategoryDto>>;

public record CategoryDto(
    Guid Id,
    string Name,
    string? Icon,
    string? Color,
    int TypeCount,
    int TransactionCount
);

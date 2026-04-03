namespace BudgetBuddy.API.VSA.Features.CategoryTypes.GetCategoryTypes;

public record GetCategoryTypesQuery(
    Guid? CategoryId,
    int Page = 1,
    int PageSize = 10
) : IRequest<GetCategoryTypesResponse>;

public record GetCategoryTypesResponse(
    List<CategoryTypeDto> CategoryTypes,
    int TotalCount,
    int Page,
    int PageSize
);

public record CategoryTypeDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Name,
    string? Icon,
    string? Color
);

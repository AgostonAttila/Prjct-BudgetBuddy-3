using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Application.Features.CategoryTypes.GetCategoryTypes;

public class GetCategoryTypesHandler(
    ICategoryTypeRepository categoryTypeRepo,
    ICurrentUserService currentUserService) : UserAwareHandler<GetCategoryTypesQuery, GetCategoryTypesResponse>(currentUserService)
{
    public override async Task<GetCategoryTypesResponse> Handle(
        GetCategoryTypesQuery request,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await categoryTypeRepo.GetPagedAsync(
            UserId, request.CategoryId, request.Page, request.PageSize, cancellationToken);

        var categoryTypeDtos = items
            .Select(ct => new CategoryTypeDto(ct.Id, ct.CategoryId, ct.Category.Name, ct.Name, ct.Icon, ct.Color))
            .ToList();

        return new GetCategoryTypesResponse(
            CategoryTypes: categoryTypeDtos,
            TotalCount: totalCount,
            Page: request.Page,
            PageSize: request.PageSize
        );
    }
}

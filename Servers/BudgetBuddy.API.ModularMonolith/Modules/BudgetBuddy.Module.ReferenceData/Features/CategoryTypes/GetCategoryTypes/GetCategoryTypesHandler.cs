using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;

namespace BudgetBuddy.Module.ReferenceData.Features.CategoryTypes.GetCategoryTypes;

public class GetCategoryTypesHandler(
    ReferenceDataDbContext context,
    ICurrentUserService currentUserService) : UserAwareHandler<GetCategoryTypesQuery, GetCategoryTypesResponse>(currentUserService)
{
    public override async Task<GetCategoryTypesResponse> Handle(
        GetCategoryTypesQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.CategoryTypes
            .AsNoTracking()
            .Where(ct => ct.Category.UserId == UserId);

        // Filter by category if provided
        if (request.CategoryId.HasValue)
            query = query.Where(ct => ct.CategoryId == request.CategoryId.Value);
        
        var totalCount = await query.CountAsync(cancellationToken);

        return new GetCategoryTypesResponse(
            CategoryTypes: await GetCategoryTypes(request, query, cancellationToken),
            TotalCount: totalCount,
            Page: request.Page,
            PageSize: request.PageSize
        );
    }

    private static async Task<List<CategoryTypeDto>> GetCategoryTypes(GetCategoryTypesQuery request, IQueryable<CategoryType> query, CancellationToken cancellationToken)
    {
        return await query
            .OrderBy(ct => ct.Category.Name)
            .ThenBy(ct => ct.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(ct => new CategoryTypeDto(
                ct.Id,
                ct.CategoryId,
                ct.Category.Name,          
                ct.Name,
                ct.Icon,
                ct.Color
            ))
            .ToListAsync(cancellationToken);
    }
}

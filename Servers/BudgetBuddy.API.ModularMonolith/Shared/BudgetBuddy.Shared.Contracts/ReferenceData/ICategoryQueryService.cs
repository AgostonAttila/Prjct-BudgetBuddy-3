namespace BudgetBuddy.Shared.Contracts.ReferenceData;

public record CategoryInfo(
    Guid Id,
    string Name,
    string? Icon);

public interface ICategoryQueryService
{
    Task<Dictionary<Guid, CategoryInfo>> GetCategoriesByIdsAsync(
        IEnumerable<Guid> categoryIds,
        CancellationToken cancellationToken = default);

    Task<Dictionary<string, Guid>> GetUserCategoryNameMapAsync(
        string userId,
        CancellationToken cancellationToken = default);
}

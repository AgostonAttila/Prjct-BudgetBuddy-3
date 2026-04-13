using BudgetBuddy.Application.Common.Contracts;

namespace BudgetBuddy.Application.Common.Repositories;

public record InvestmentFilter(
    string UserId,
    InvestmentType? Type,
    string? SearchTerm,
    int Page,
    int PageSize);

public record ExportInvestmentFilter(
    string UserId,
    InvestmentType? Type,
    string? Search);

public record InvestmentPageItem(
    Guid Id,
    string Symbol,
    string Name,
    InvestmentType Type,
    decimal Quantity,
    decimal PurchasePrice,
    string CurrencyCode,
    LocalDate PurchaseDate,
    string? Note,
    string? AccountName);

public interface IInvestmentRepository : IUserOwnedRepository<Investment>
{
    Task<(List<InvestmentPageItem> Items, int TotalCount)> GetPagedAsync(InvestmentFilter filter, CancellationToken ct = default);
    Task<List<Investment>> GetForExportAsync(ExportInvestmentFilter filter, CancellationToken ct = default);
    Task<BatchDeleteResult> BatchDeleteAsync(List<Guid> ids, string userId, string entityName, CancellationToken ct = default);
}

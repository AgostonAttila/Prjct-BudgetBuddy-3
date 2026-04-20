using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;

namespace BudgetBuddy.Module.Investments.Features.GetInvestments;

public class GetInvestmentsHandler(
    InvestmentsDbContext _context,
    ICurrentUserService currentUserService,
    ILogger<GetInvestmentsHandler> _logger) : UserAwareHandler<GetInvestmentsQuery, GetInvestmentsResponse>(currentUserService)
{
    public override async Task<GetInvestmentsResponse> Handle(
        GetInvestmentsQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching investments for user {UserId}", UserId);

        var query = _context.Investments
            .Where(i => i.UserId == UserId);

        if (request.Type.HasValue)
            query = query.Where(i => i.Type == request.Type.Value);

        // Apply search term using PostgreSQL Full-Text Search (fast with GIN index)
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            // Sanitize search term: replace spaces with & for AND logic
            var searchQuery = string.Join(" & ", request.SearchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries));

            query = query.Where(i =>
                EF.Functions.ToTsVector("english",
                    i.Symbol + " " + i.Name + " " + (i.Note ?? "")
                ).Matches(EF.Functions.ToTsQuery("english", searchQuery))
            );
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Optimized: Removed Include, navigation properties in Select translate to SQL JOINs
        var investments = await query
            .AsNoTracking()
            .OrderBy(i => i.Symbol)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(i => new InvestmentDto(
                i.Id,
                i.Symbol,
                i.Name,
                i.Type,
                i.Quantity,
                i.PurchasePrice,
                i.CurrencyCode,
                i.PurchaseDate,
                i.Note,
                null // AccountName: cross-module navigation removed — AccountId FK still available
            ))
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Found {Count} investments (total {TotalCount}) for user {UserId}",
            investments.Count,
            totalCount,
            UserId);

        return new GetInvestmentsResponse(
            investments,
            totalCount,
            request.PageNumber,
            request.PageSize
        );
    }
}

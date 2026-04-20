using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.DataExchange;
using BudgetBuddy.Shared.Infrastructure.Handlers;

namespace BudgetBuddy.Module.Investments.Features.ExportInvestments;

public class ExportInvestmentsHandler(
    InvestmentsDbContext context,
    ICurrentUserService currentUserService,
    IExportFactory exportFactory) : UserAwareHandler<ExportInvestmentsQuery, ExportInvestmentsResponse>(currentUserService)
{
    public override async Task<ExportInvestmentsResponse> Handle(
        ExportInvestmentsQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.Investments
            .AsNoTracking()
            .Where(i => i.UserId == UserId);

        if (request.Type.HasValue)
            query = query.Where(i => i.Type == request.Type.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLower();
            query = query.Where(i =>
                i.Symbol.ToLower().Contains(searchLower) ||
                i.Name.ToLower().Contains(searchLower) ||
                (i.Note != null && i.Note.ToLower().Contains(searchLower))
            );
        }

        var investments = await query
            .OrderBy(i => i.Type)
            .ThenBy(i => i.Symbol)
            .ToListAsync(cancellationToken);

        var columnMappings = new Dictionary<string, Func<Investment, object>>
        {
            ["Symbol"] = i => i.Symbol,
            ["Name"] = i => i.Name,
            ["Type"] = i => i.Type.ToString(),
            ["Quantity"] = i => i.Quantity,
            ["Purchase Price"] = i => i.PurchasePrice,
            ["Total Cost"] = i => i.Quantity * i.PurchasePrice,
            ["Currency"] = i => i.CurrencyCode,
            ["Purchase Date"] = i => i.PurchaseDate.ToString("yyyy-MM-dd", null),
            ["Account"] = i => i.AccountId.HasValue ? i.AccountId.Value.ToString() : "N/A", // Account name not available (cross-module navigation removed)
            ["Note"] = i => i.Note ?? ""
        };

        var result = exportFactory.Export(request.Format, investments, columnMappings, "investments", "Investments");
        return new ExportInvestmentsResponse(result.Content, result.FileName, result.ContentType);
    }
}

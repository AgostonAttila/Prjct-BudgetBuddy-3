using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.DataExchange;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Common.Shared.Handlers;

namespace BudgetBuddy.API.VSA.Features.Investments.ExportInvestments;

public class ExportInvestmentsHandler(
    AppDbContext context,
    ICurrentUserService currentUserService,
    IExportFactory exportFactory) : UserAwareHandler<ExportInvestmentsQuery, ExportInvestmentsResponse>(currentUserService)
{
    public override async Task<ExportInvestmentsResponse> Handle(
        ExportInvestmentsQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.Investments
            .AsNoTracking()
            .Include(i => i.Account)
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

        var columnMappings = new Dictionary<string, Func<Common.Domain.Entities.Investment, object>>
        {
            ["Symbol"] = i => i.Symbol,
            ["Name"] = i => i.Name,
            ["Type"] = i => i.Type.ToString(),
            ["Quantity"] = i => i.Quantity,
            ["Purchase Price"] = i => i.PurchasePrice,
            ["Total Cost"] = i => i.Quantity * i.PurchasePrice,
            ["Currency"] = i => i.CurrencyCode,
            ["Purchase Date"] = i => i.PurchaseDate.ToString("yyyy-MM-dd", null),
            ["Account"] = i => i.Account?.Name ?? "N/A",
            ["Note"] = i => i.Note ?? ""
        };

        var result = exportFactory.Export(request.Format, investments, columnMappings, "investments", "Investments");
        return new ExportInvestmentsResponse(result.Content, result.FileName, result.ContentType);
    }
}

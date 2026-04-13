using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Application.Features.Investments.ExportInvestments;

public class ExportInvestmentsHandler(
    IInvestmentRepository investmentRepo,
    ICurrentUserService currentUserService,
    IExportFactory exportFactory) : UserAwareHandler<ExportInvestmentsQuery, ExportInvestmentsResponse>(currentUserService)
{
    public override async Task<ExportInvestmentsResponse> Handle(
        ExportInvestmentsQuery request,
        CancellationToken cancellationToken)
    {
        var filter = new ExportInvestmentFilter(UserId, request.Type, request.Search);
        var investments = await investmentRepo.GetForExportAsync(filter, cancellationToken);

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
            ["Account"] = i => i.Account?.Name ?? "N/A",
            ["Note"] = i => i.Note ?? ""
        };

        var result = exportFactory.Export<Investment>(request.Format, investments, columnMappings, "investments", "Investments");
        return new ExportInvestmentsResponse(result.Content, result.FileName, result.ContentType);
    }
}

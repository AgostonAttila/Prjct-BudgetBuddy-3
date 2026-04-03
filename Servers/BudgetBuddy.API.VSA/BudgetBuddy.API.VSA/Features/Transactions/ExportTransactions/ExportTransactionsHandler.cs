using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.DataExchange;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Common.Shared.Extensions;
using BudgetBuddy.API.VSA.Common.Shared.Handlers;

namespace BudgetBuddy.API.VSA.Features.Transactions.ExportTransactions;

public class ExportTransactionsHandler(
    AppDbContext context,
    ICurrentUserService currentUserService,
    IExportFactory exportFactory) : UserAwareHandler<ExportTransactionsQuery, ExportTransactionsResponse>(currentUserService)
{
    public override async Task<ExportTransactionsResponse> Handle(
        ExportTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.Transactions
            .AsNoTracking()
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Where(t => t.UserId == UserId);

        if (request.AccountId.HasValue)
            query = query.Where(t => t.AccountId == request.AccountId.Value);

        if (request.CategoryId.HasValue)
            query = query.Where(t => t.CategoryId == request.CategoryId.Value);

        if (request.TransactionType.HasValue)
            query = query.Where(t => t.TransactionType == request.TransactionType.Value);

        if (!string.IsNullOrWhiteSpace(request.StartDate))
        {
            var start = request.StartDate.ParseIsoDateOrThrow(nameof(request.StartDate));
            query = query.Where(t => t.TransactionDate >= start);
        }

        if (!string.IsNullOrWhiteSpace(request.EndDate))
        {
            var end = request.EndDate.ParseIsoDateOrThrow(nameof(request.EndDate));
            query = query.Where(t => t.TransactionDate <= end);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLower();
            query = query.Where(t =>
                (t.Note != null && t.Note.ToLower().Contains(searchLower)) ||
                (t.Payee != null && t.Payee.ToLower().Contains(searchLower)) ||
                (t.Labels != null && t.Labels.ToLower().Contains(searchLower))
            );
        }

        var transactions = await query
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        var columnMappings = new Dictionary<string, Func<Common.Domain.Entities.Transaction, object>>
        {
            ["Date"] = t => t.TransactionDate.ToString("yyyy-MM-dd", null),
            ["Account"] = t => t.Account?.Name ?? "N/A",
            ["Category"] = t => t.Category?.Name ?? "Uncategorized",
            ["Type"] = t => t.TransactionType.ToString(),
            ["Payment Method"] = t => t.PaymentType.ToString(),
            ["Amount"] = t => t.Amount,
            ["Currency"] = t => t.CurrencyCode,
            ["Payee"] = t => t.Payee ?? "",
            ["Note"] = t => t.Note ?? "",
            ["Labels"] = t => t.Labels ?? "",
            ["Is Transfer"] = t => t.IsTransfer ? "Yes" : "No"
        };

        var result = exportFactory.Export(request.Format, transactions, columnMappings, "transactions", "Transactions");
        return new ExportTransactionsResponse(result.Content, result.FileName, result.ContentType);
    }
}

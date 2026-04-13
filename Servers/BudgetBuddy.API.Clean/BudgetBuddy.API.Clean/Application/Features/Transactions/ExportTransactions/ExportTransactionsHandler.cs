using BudgetBuddy.Application.Common.Extensions;
using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Application.Features.Transactions.ExportTransactions;

public class ExportTransactionsHandler(
    ITransactionRepository transactionRepo,
    ICurrentUserService currentUserService,
    IExportFactory exportFactory) : UserAwareHandler<ExportTransactionsQuery, ExportTransactionsResponse>(currentUserService)
{
    public override async Task<ExportTransactionsResponse> Handle(
        ExportTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        LocalDate? startDate = null;
        if (!string.IsNullOrWhiteSpace(request.StartDate))
            startDate = request.StartDate.ParseIsoDateOrThrow(nameof(request.StartDate));

        LocalDate? endDate = null;
        if (!string.IsNullOrWhiteSpace(request.EndDate))
            endDate = request.EndDate.ParseIsoDateOrThrow(nameof(request.EndDate));

        var filter = new ExportTransactionFilter(
            UserId,
            request.AccountId,
            request.CategoryId,
            request.TransactionType,
            startDate,
            endDate,
            request.Search);

        var transactions = await transactionRepo.GetForExportAsync(filter, cancellationToken);

        var columnMappings = new Dictionary<string, Func<Transaction, object>>
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

        var result = exportFactory.Export<Transaction>(request.Format, transactions, columnMappings, "transactions", "Transactions");
        return new ExportTransactionsResponse(result.Content, result.FileName, result.ContentType);
    }
}

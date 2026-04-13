using BudgetBuddy.Domain.Enums;

namespace BudgetBuddy.Application.Features.Transactions.ExportTransactions;

public record ExportTransactionsQuery(
    Guid? AccountId,
    Guid? CategoryId,
    TransactionType? TransactionType,
    string? StartDate,
    string? EndDate,
    string? Search,
    ExportFormat Format
) : IRequest<ExportTransactionsResponse>;

public record ExportTransactionsResponse(
    byte[] FileContent,
    string FileName,
    string ContentType
);

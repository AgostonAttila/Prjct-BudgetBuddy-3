namespace BudgetBuddy.Application.Features.Transactions.GetTransactions;

public record GetTransactionsQuery(
    Guid? AccountId = null,
    Guid? CategoryId = null,
    LocalDate? StartDate = null,
    LocalDate? EndDate = null,
    TransactionType? Type = null,
    string? SearchTerm = null, // Search in Note, Payee, Labels
    int PageNumber = 1,
    int PageSize = 50
) : IRequest<GetTransactionsResponse>;

public record GetTransactionsResponse(
    List<TransactionDto> Transactions,
    int TotalCount,
    int PageNumber,
    int PageSize
);

// Cache-safe DTO - excludes PII fields (Note, Payee) for security
public record TransactionDto(
    Guid Id,
    string AccountName,
    string? CategoryName,
    decimal Amount,
    string CurrencyCode,
    TransactionType TransactionType,
    PaymentType PaymentType,
    LocalDate TransactionDate,
    bool IsTransfer
    // ⚠️ Note and Payee excluded - PII data should not be cached
);

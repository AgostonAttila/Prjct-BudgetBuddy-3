namespace BudgetBuddy.Application.Common.Repositories;

public record TransactionFilter(
    string UserId,
    Guid? AccountId,
    Guid? CategoryId,
    LocalDate? StartDate,
    LocalDate? EndDate,
    TransactionType? Type,
    string? SearchTerm,
    int Page,
    int PageSize);

public record ExportTransactionFilter(
    string UserId,
    Guid? AccountId,
    Guid? CategoryId,
    TransactionType? TransactionType,
    LocalDate? StartDate,
    LocalDate? EndDate,
    string? Search);

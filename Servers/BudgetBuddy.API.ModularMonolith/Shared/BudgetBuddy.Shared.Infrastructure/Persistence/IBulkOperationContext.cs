namespace BudgetBuddy.Shared.Infrastructure.Persistence;

/// <summary>
/// Opt-in interface for DbContexts that support bulk operations.
/// Set <see cref="BulkOperationInProgress"/> to true before calling BulkInsertAsync/BulkUpdateAsync
/// to suppress audit logging for the duration of the operation.
/// </summary>
public interface IBulkOperationContext
{
    bool BulkOperationInProgress { get; set; }
}

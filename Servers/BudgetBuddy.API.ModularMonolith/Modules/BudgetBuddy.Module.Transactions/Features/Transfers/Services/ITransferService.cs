namespace BudgetBuddy.Module.Transactions.Features.Transfers.Services;

/// <summary>
/// Service for handling transfer operations between accounts
/// </summary>
public interface ITransferService
{
    /// <summary>
    /// Creates a transfer between two accounts by creating paired transactions
    /// </summary>
    Task<(Guid FromTransactionId, Guid ToTransactionId)> CreateTransferAsync(
        string userId,
        Guid fromAccountId,
        Guid toAccountId,
        decimal amount,
        string currencyCode,
        LocalDate transferDate,
        PaymentType paymentType,
        string? note = null,
        CancellationToken cancellationToken = default);
}

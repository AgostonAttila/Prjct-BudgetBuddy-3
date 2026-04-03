using NodaTime;

namespace BudgetBuddy.API.VSA.Common.Shared.Contracts;

/// <summary>
/// Service for calculating account balances with multi-currency support
/// </summary>
public interface IAccountBalanceService
{
    /// <summary>
    /// Calculate balance breakdown for all user accounts
    /// </summary>
    Task<List<AccountBalanceResult>> CalculateAccountBalancesAsync(
        string userId,
        string targetCurrency = "USD",
        LocalDate? upToDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate total balance across all user accounts
    /// </summary>
    Task<decimal> CalculateTotalBalanceAsync(
        string userId,
        string targetCurrency = "USD",
        LocalDate? upToDate = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Account balance calculation result
/// </summary>
public record AccountBalanceResult(
    Guid AccountId,
    string AccountName,
    string CurrencyCode,
    decimal Balance,
    decimal ConvertedBalance);

using NodaTime;

namespace BudgetBuddy.Shared.Contracts.Accounts;

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

    /// <summary>
    /// Returns the number of accounts belonging to the user
    /// </summary>
    Task<int> GetAccountCountAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the initial (opening) balance and currency for each account, without transaction deltas applied
    /// </summary>
    Task<List<AccountInitialBalanceResult>> GetAccountInitialBalancesAsync(
        string userId,
        Guid? accountId = null,
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

/// <summary>
/// Raw opening balance for an account, without transaction deltas applied
/// </summary>
public record AccountInitialBalanceResult(
    Guid AccountId,
    decimal InitialBalance,
    string CurrencyCode);

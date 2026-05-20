using NodaTime;

namespace BudgetBuddy.Shared.Messages.Contracts.Accounts;

public interface IAccountBalanceService
{
    Task<List<AccountBalanceResult>> CalculateAccountBalancesAsync(
        string userId,
        string targetCurrency = "USD",
        LocalDate? upToDate = null,
        CancellationToken cancellationToken = default);

    Task<decimal> CalculateTotalBalanceAsync(
        string userId,
        string targetCurrency = "USD",
        LocalDate? upToDate = null,
        CancellationToken cancellationToken = default);

    Task<int> GetAccountCountAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<List<AccountInitialBalanceResult>> GetAccountInitialBalancesAsync(
        string userId,
        Guid? accountId = null,
        CancellationToken cancellationToken = default);
}

public record AccountBalanceResult(
    Guid AccountId,
    string AccountName,
    string CurrencyCode,
    decimal Balance,
    decimal ConvertedBalance);

public record AccountInitialBalanceResult(
    Guid AccountId,
    decimal InitialBalance,
    string CurrencyCode);

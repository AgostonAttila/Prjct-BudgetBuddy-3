using BudgetBuddy.Shared.Contracts.Accounts;


namespace BudgetBuddy.Module.Accounts.Services;

/// <summary>
/// Service for calculating account balances with multi-currency support
/// Consolidates account balance calculation logic used across multiple features
/// </summary>
public class AccountBalanceService(
    AccountsDbContext context,
    IAccountTransactionSummary transactionSummary,
    ICurrencyConversionService currencyConversionService,
    ILogger<AccountBalanceService> logger)
    : CurrencyServiceBase(currencyConversionService, logger), IAccountBalanceService
{
    public async Task<List<AccountBalanceResult>> CalculateAccountBalancesAsync(
        string userId,
        string targetCurrency = "USD",
        LocalDate? upToDate = null,
        CancellationToken cancellationToken = default)
    {
        targetCurrency = targetCurrency.ToUpperInvariant();

        // Load accounts with projection
        var accounts = await context.Accounts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .Select(a => new
            {
                a.Id,
                a.Name,
                a.InitialBalance,
                CurrencyCode = a.DefaultCurrencyCode ?? "USD"
            })
            .ToListAsync(cancellationToken);

        if (accounts.Count == 0)
            return [];

        var accountIds = accounts.Select(a => a.Id).ToList();

        // Query transaction aggregates via cross-module contract (Transactions module implements this)
        var aggregates = await transactionSummary.GetAggregatesForAccountsAsync(accountIds, upToDate, cancellationToken);

        // Create lookup for O(1) access
        var transactionLookup = aggregates.ToDictionary(a => a.AccountId);

        // Get exchange rates for unique currencies (batch operation)
        var uniqueCurrencies = accounts
            .Select(a => a.CurrencyCode.ToUpperInvariant())
            .Distinct()
            .ToList();

        var exchangeRates = await GetExchangeRatesAsync(uniqueCurrencies, targetCurrency, cancellationToken);

        // Calculate balances with currency conversion
        return accounts
            .Select(account =>
            {
                var aggregate = transactionLookup.GetValueOrDefault(account.Id);
                var totalIncome = aggregate?.TotalIncome ?? 0;
                var totalExpense = aggregate?.TotalExpense ?? 0;

                var balance = account.InitialBalance + totalIncome - totalExpense;
                var currencyCode = account.CurrencyCode.ToUpperInvariant();
                var exchangeRate = exchangeRates.GetValueOrDefault(currencyCode, 1m);
                var convertedBalance = balance * exchangeRate;

                return new AccountBalanceResult(
                    AccountId: account.Id,
                    AccountName: account.Name,
                    CurrencyCode: currencyCode,
                    Balance: Math.Round(balance, 2),
                    ConvertedBalance: Math.Round(convertedBalance, 2)
                );
            })
            .ToList();
    }

    public async Task<decimal> CalculateTotalBalanceAsync(
        string userId,
        string targetCurrency = "USD",
        LocalDate? upToDate = null,
        CancellationToken cancellationToken = default)
    {
        var balances = await CalculateAccountBalancesAsync(userId, targetCurrency, upToDate, cancellationToken);
        return Math.Round(balances.Sum(b => b.ConvertedBalance), 2);
    }

    public Task<int> GetAccountCountAsync(
        string userId,
        CancellationToken cancellationToken = default) =>
        context.Accounts
            .AsNoTracking()
            .CountAsync(a => a.UserId == userId, cancellationToken);

    public async Task<List<AccountInitialBalanceResult>> GetAccountInitialBalancesAsync(
        string userId,
        Guid? accountId = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Accounts
            .AsNoTracking()
            .Where(a => a.UserId == userId);

        if (accountId.HasValue)
            query = query.Where(a => a.Id == accountId.Value);

        return await query
            .Select(a => new AccountInitialBalanceResult(
                a.Id,
                a.InitialBalance,
                (a.DefaultCurrencyCode ?? "USD").ToUpperInvariant()))
            .ToListAsync(cancellationToken);
    }
}

using BudgetBuddy.API.VSA.Common.Infrastructure.Financial;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace BudgetBuddy.API.VSA.Common.Shared.Services;

/// <summary>
/// Service for calculating account balances with multi-currency support
/// Consolidates account balance calculation logic used across multiple features
/// </summary>
public class AccountBalanceService(
    AppDbContext context,
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

        // Build transaction query with optional date filter
        var transactionQuery = context.Transactions
            .AsNoTracking()
            .Where(t => accountIds.Contains(t.AccountId));

        if (upToDate.HasValue)
            transactionQuery = transactionQuery.Where(t => t.TransactionDate <= upToDate.Value);

        // Aggregate transactions by account and type
        var transactionAggregates = await transactionQuery
            .GroupBy(t => new { t.AccountId, t.TransactionType })
            .Select(g => new
            {
                g.Key.AccountId,
                g.Key.TransactionType,
                Total = g.Sum(t => t.Amount)
            })
            .ToListAsync(cancellationToken);

        // Create lookup for O(1) access
        var transactionLookup = transactionAggregates
            .GroupBy(t => t.AccountId)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    Income = g.Where(x => x.TransactionType == TransactionType.Income).Sum(x => x.Total),
                    Expense = g.Where(x => x.TransactionType == TransactionType.Expense).Sum(x => x.Total)
                });

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
                var transactionData = transactionLookup.GetValueOrDefault(account.Id);
                var totalIncome = transactionData?.Income ?? 0;
                var totalExpense = transactionData?.Expense ?? 0;

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
}

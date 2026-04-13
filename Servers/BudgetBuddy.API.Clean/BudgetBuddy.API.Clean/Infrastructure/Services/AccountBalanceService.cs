using BudgetBuddy.Application.Common.Contracts;
using BudgetBuddy.Infrastructure.Financial;

namespace BudgetBuddy.Infrastructure.Services;

public class AccountBalanceService(
    IAccountRepository accountRepo,
    ITransactionRepository transactionRepo,
    CurrencyConverter currencyConverter,
    ILogger<AccountBalanceService> logger) : IAccountBalanceService
{
    public async Task<List<AccountBalanceResult>> CalculateAccountBalancesAsync(
        string userId,
        string targetCurrency = "USD",
        LocalDate? upToDate = null,
        CancellationToken cancellationToken = default)
    {
        targetCurrency = targetCurrency.ToUpperInvariant();

        var accounts = await accountRepo.GetBalanceDataAsync(userId, cancellationToken);
        if (accounts.Count == 0)
            return [];

        var accountIds = accounts.Select(a => a.Id).ToList();
        var aggregates = await transactionRepo.GetBalanceAggregatesByAccountAsync(
            accountIds, upToDate, cancellationToken);

        var transactionLookup = aggregates
            .GroupBy(t => t.AccountId)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    Income = g.Where(x => x.Type == TransactionType.Income).Sum(x => x.Total),
                    Expense = g.Where(x => x.Type == TransactionType.Expense).Sum(x => x.Total)
                });

        var uniqueCurrencies = accounts
            .Select(a => a.CurrencyCode.ToUpperInvariant())
            .Distinct()
            .ToList();

        var exchangeRates = await currencyConverter.GetExchangeRatesAsync(
            uniqueCurrencies, targetCurrency, cancellationToken);

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

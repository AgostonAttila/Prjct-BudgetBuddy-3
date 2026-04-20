using BudgetBuddy.Module.Analytics.Features.Reports.GetIncomeVsExpense;
using BudgetBuddy.Module.Analytics.Features.Reports.GetInvestmentPerformance;
using BudgetBuddy.Module.Analytics.Features.Reports.GetMonthlySummary;
using BudgetBuddy.Module.Analytics.Features.Reports.GetSpendingByCategory;
using BudgetBuddy.Shared.Contracts.Investments;
using BudgetBuddy.Shared.Contracts.Transactions;
using BudgetBuddy.Shared.Infrastructure.Financial;
using System.Globalization;

namespace BudgetBuddy.Module.Analytics.Features.Reports.Services;

public class ReportService(
    ITransactionQueryService transactionQueryService,
    IAccountBalanceService accountBalanceService,
    IInvestmentDataService investmentDataService,
    IClock clock,
    IInvestmentCalculationService investmentCalculationService,
    ICurrencyConversionService currencyConversionService,
    ILogger<ReportService> logger) : CurrencyServiceBase(currencyConversionService, logger), IReportService
{
    private const int TopCategoriesLimit = 5;

    public async Task<(decimal TotalIncome, decimal TotalExpense, decimal NetIncome, int IncomeCount, int ExpenseCount)>
        CalculateIncomeVsExpenseAsync(
            string userId,
            LocalDate? startDate = null,
            LocalDate? endDate = null,
            Guid? accountId = null,
            string displayCurrency = "USD",
            CancellationToken cancellationToken = default)
    {
        displayCurrency = displayCurrency.ToUpperInvariant();

        var groupedByCurrency = await transactionQueryService
            .GetTotalsByCurrencyAndTypeAsync(userId, startDate, endDate, accountId, cancellationToken);

        if (groupedByCurrency.Count == 0)
            return (0, 0, 0, 0, 0);

        var exchangeRates = await GetExchangeRatesFromCollectionAsync(
            groupedByCurrency, g => g.CurrencyCode, displayCurrency, cancellationToken);

        var (totalIncome, totalExpense, incomeCount, expenseCount) = groupedByCurrency
            .AggregateIncomeExpenseWithCount(
                g => g.TotalAmount,
                g => g.CurrencyCode,
                g => g.TransactionType,
                g => g.Count,
                exchangeRates);

        return (
            Math.Round(totalIncome, 2),
            Math.Round(totalExpense, 2),
            Math.Round(totalIncome - totalExpense, 2),
            incomeCount,
            expenseCount
        );
    }

    public async Task<List<MonthlyBreakdown>> GetMonthlyBreakdownAsync(
        string userId,
        LocalDate? startDate = null,
        LocalDate? endDate = null,
        Guid? accountId = null,
        string displayCurrency = "USD",
        CancellationToken cancellationToken = default)
    {
        displayCurrency = displayCurrency.ToUpperInvariant();

        var groupedData = await transactionQueryService
            .GetGroupedByMonthCurrencyTypeAsync(userId, startDate, endDate, accountId, cancellationToken);

        if (groupedData.Count == 0)
            return [];

        var exchangeRates = await GetExchangeRatesFromCollectionAsync(
            groupedData, g => g.CurrencyCode, displayCurrency, cancellationToken);

        return groupedData
            .GroupBy(g => new { g.Year, g.Month })
            .Select(monthGroup =>
            {
                var (income, expense) = monthGroup.AggregateIncomeExpense(
                    x => x.TotalAmount,
                    x => x.CurrencyCode,
                    x => x.TransactionType,
                    exchangeRates);

                return new MonthlyBreakdown(
                    Year: monthGroup.Key.Year,
                    Month: monthGroup.Key.Month,
                    MonthName: CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(monthGroup.Key.Month),
                    Income: Math.Round(income, 2),
                    Expense: Math.Round(expense, 2),
                    Net: Math.Round(income - expense, 2)
                );
            })
            .OrderBy(m => m.Year)
            .ThenBy(m => m.Month)
            .ToList();
    }

    public async Task<(decimal TotalSpending, List<CategorySpending> Categories)>
        CalculateSpendingByCategoryAsync(
            string userId,
            LocalDate? startDate = null,
            LocalDate? endDate = null,
            Guid? accountId = null,
            string displayCurrency = "USD",
            CancellationToken cancellationToken = default)
    {
        displayCurrency = displayCurrency.ToUpperInvariant();

        var groupedData = await transactionQueryService
            .GetExpensesGroupedByCategoryForReportAsync(userId, startDate, endDate, accountId, cancellationToken);

        if (groupedData.Count == 0)
            return (0, new List<CategorySpending>());

        var exchangeRates = await GetExchangeRatesFromCollectionAsync(
            groupedData, g => g.CurrencyCode, displayCurrency, cancellationToken);

        var categoryTotals = groupedData
            .GroupBy(g => g.CategoryId)
            .Select(catGroup => (
                CategoryId: catGroup.Key,
                CategoryName: "Uncategorized",
                Amount: catGroup.Sum(g => g.Amount * exchangeRates.GetValueOrDefault(g.CurrencyCode, 1m)),
                Count: catGroup.Sum(g => g.Count)
            ))
            .ToList();

        var totalSpending = Math.Round(categoryTotals.Sum(c => c.Amount), 2);

        var categories = categoryTotals
            .Select(c => new CategorySpending(
                c.CategoryId,
                c.CategoryName,
                Math.Round(c.Amount, 2),
                c.Count,
                totalSpending > 0 ? Math.Round((c.Amount / totalSpending) * 100, 2) : 0
            ))
            .OrderByDescending(c => c.Amount)
            .ToList();

        return (totalSpending, categories);
    }

    public async Task<(decimal TotalIncome, decimal TotalExpense, decimal NetIncome, decimal StartingBalance, decimal EndingBalance, int TotalTransactions, List<CategorySummary> TopCategories, List<DailySummary> DailyBreakdown)>
        CalculateMonthlySummaryAsync(
            string userId,
            int year,
            int month,
            Guid? accountId = null,
            string displayCurrency = "USD",
            CancellationToken cancellationToken = default)
    {
        displayCurrency = displayCurrency.ToUpperInvariant();
        var startDate = new LocalDate(year, month, 1);
        var endDate = startDate.PlusMonths(1).PlusDays(-1);

        var (summaryData, topCategoriesData, dailyData, balanceData, exchangeRates) =
            await LoadMonthlySummaryDataAsync(userId, startDate, endDate, accountId, displayCurrency, cancellationToken);

        var (totalIncome, totalExpense, incomeCount, expenseCount) = summaryData
            .AggregateIncomeExpenseWithCount(
                s => s.TotalAmount,
                s => s.CurrencyCode,
                s => s.TransactionType,
                s => s.Count,
                exchangeRates);

        var totalTransactions = incomeCount + expenseCount;
        var netIncome = totalIncome - totalExpense;

        var topCategories = ProcessTopCategories(topCategoriesData, exchangeRates);
        var dailyBreakdown = ProcessDailyBreakdown(dailyData, exchangeRates);

        return (
            Math.Round(totalIncome, 2),
            Math.Round(totalExpense, 2),
            Math.Round(netIncome, 2),
            balanceData.StartingBalance,
            balanceData.EndingBalance,
            totalTransactions,
            topCategories,
            dailyBreakdown);
    }

    public async Task<(decimal TotalInvested, decimal CurrentValue, decimal TotalGainLoss, decimal TotalGainLossPercentage, List<InvestmentPerformance> Investments)>
        CalculateInvestmentPerformanceAsync(
            string userId,
            LocalDate? startDate = null,
            LocalDate? endDate = null,
            InvestmentType? type = null,
            string displayCurrency = "USD",
            CancellationToken cancellationToken = default)
    {
        displayCurrency = displayCurrency.ToUpperInvariant();

        var investments = await investmentDataService
            .GetActiveInvestmentsAsync(userId, startDate, endDate, type, cancellationToken);

        if (!investments.Any())
            return (0, 0, 0, 0, new List<InvestmentPerformance>());

        var symbolsWithTypes = investments
            .Select(i => (i.Symbol, i.Type))
            .Distinct()
            .ToList();

        var exchangeRates = await GetExchangeRatesFromCollectionAsync(
            investments, i => i.CurrencyCode, displayCurrency, cancellationToken);

        var purchasePriceFallback = investments
            .GroupBy(i => i.Symbol)
            .ToDictionary(
                g => g.Key,
                g => g.Average(i => i.PurchasePrice * exchangeRates.GetValueOrDefault(i.CurrencyCode, 1m)));

        var currentPrices = await investmentCalculationService.GetCurrentPricesAsync(
            symbolsWithTypes, displayCurrency, purchasePriceFallback, cancellationToken);

        var today = clock.GetCurrentInstant().InUtc().Date;

        var minPurchaseDate = investments.Min(i => i.PurchaseDate);
        var historicalRates = await LoadHistoricalRatesAsync(minPurchaseDate, today, cancellationToken);

        var performanceList = investments.Select(i =>
        {
            var historicalRate = GetHistoricalCrossRate(
                historicalRates, i.CurrencyCode, displayCurrency, i.PurchaseDate,
                fallback: exchangeRates.GetValueOrDefault(i.CurrencyCode, 1m));
            var purchasePrice = i.PurchasePrice * historicalRate;

            var currentPrice = currentPrices.GetValueOrDefault(i.Symbol, purchasePrice);
            var daysHeld = Period.Between(i.PurchaseDate, today, PeriodUnits.Days).Days;

            var (totalInvested, currentValue, gainLoss, gainLossPercentage) =
                investmentCalculationService.CalculateInvestmentMetrics(
                    i.Quantity, purchasePrice, currentPrice);

            return new InvestmentPerformance(
                i.Id,
                i.Symbol,
                i.Name,
                i.Type,
                i.Quantity,
                Math.Round(purchasePrice, 2),
                totalInvested,
                Math.Round(currentPrice, 2),
                currentValue,
                gainLoss,
                gainLossPercentage,
                daysHeld,
                i.PurchaseDate
            );
        }).ToList();

        var totalInvested = performanceList.Sum(p => p.TotalInvested);
        var currentValue = performanceList.Sum(p => p.CurrentValue);
        var totalGainLoss = currentValue - totalInvested;
        var totalGainLossPercentage = totalInvested > 0
            ? Math.Round((totalGainLoss / totalInvested) * 100, 2)
            : 0;

        return (
            Math.Round(totalInvested, 2),
            Math.Round(currentValue, 2),
            Math.Round(totalGainLoss, 2),
            totalGainLossPercentage,
            performanceList.OrderByDescending(p => p.GainLossPercentage).ToList()
        );
    }

    private async Task<Dictionary<string, SortedList<LocalDate, decimal>>> LoadHistoricalRatesAsync(
        LocalDate from, LocalDate to, CancellationToken cancellationToken)
    {
        var snapshots = await investmentDataService.GetExchangeRateSnapshotsAsync(from, to, cancellationToken);

        var result = new Dictionary<string, SortedList<LocalDate, decimal>>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in snapshots)
        {
            var key = s.Currency.ToUpperInvariant();
            if (!result.TryGetValue(key, out var sl))
            {
                sl = new SortedList<LocalDate, decimal>();
                result[key] = sl;
            }
            sl[s.Date] = s.RateToUsd;
        }
        return result;
    }

    private static decimal GetHistoricalCrossRate(
        Dictionary<string, SortedList<LocalDate, decimal>> historicalRates,
        string fromCurrency,
        string toCurrency,
        LocalDate date,
        decimal fallback)
    {
        if (fromCurrency.Equals(toCurrency, StringComparison.OrdinalIgnoreCase))
            return 1m;

        var fromRate = GetLatestRateOnOrBefore(historicalRates, fromCurrency, date);
        var toRate = GetLatestRateOnOrBefore(historicalRates, toCurrency, date);

        if (fromRate is null || toRate is null || toRate == 0m)
            return fallback;

        return fromRate.Value / toRate.Value;
    }

    // O(log n) binary search instead of O(n) linear scan
    private static decimal? GetLatestRateOnOrBefore(
        Dictionary<string, SortedList<LocalDate, decimal>> historicalRates,
        string currency,
        LocalDate date)
    {
        if (!historicalRates.TryGetValue(currency.ToUpperInvariant(), out var rates) || rates.Count == 0)
            return null;

        var keys = rates.Keys;
        int lo = 0, hi = keys.Count - 1, found = -1;
        while (lo <= hi)
        {
            var mid = (lo + hi) / 2;
            if (keys[mid] <= date)
            {
                found = mid;
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        return found >= 0 ? rates.Values[found] : null;
    }

    private record MonthlySummaryRawData(
        TransactionType TransactionType,
        string CurrencyCode,
        decimal TotalAmount,
        int Count);

    private record CategoryRawData(
        string CategoryName,
        string CurrencyCode,
        decimal Amount,
        int Count);

    private record DailyRawData(
        int Day,
        TransactionType TransactionType,
        string CurrencyCode,
        decimal Amount,
        int Count);

    private async Task<(
        List<MonthlySummaryRawData> SummaryData,
        List<CategoryRawData> TopCategoriesData,
        List<DailyRawData> DailyData,
        (decimal StartingBalance, decimal EndingBalance) BalanceData,
        Dictionary<string, decimal> ExchangeRates)>
        LoadMonthlySummaryDataAsync(
            string userId,
            LocalDate startDate,
            LocalDate endDate,
            Guid? accountId,
            string displayCurrency,
            CancellationToken cancellationToken)
    {
        var summaryRows = await transactionQueryService
            .GetMonthlySummaryGroupedAsync(userId, startDate, endDate, accountId, cancellationToken);
        var summaryData = summaryRows
            .Select(r => new MonthlySummaryRawData(r.TransactionType, r.CurrencyCode, r.TotalAmount, r.Count))
            .ToList();

        // Top categories: group all expenses under "Uncategorized" (cross-module navigation removed)
        var topCategoriesData = summaryRows
            .Where(r => r.TransactionType == TransactionType.Expense)
            .Select(r => new CategoryRawData("Uncategorized", r.CurrencyCode, r.TotalAmount, r.Count))
            .ToList();

        var dailyRows = await transactionQueryService
            .GetDailyBreakdownAsync(userId, startDate, endDate, accountId, cancellationToken);
        var dailyData = dailyRows
            .Select(r => new DailyRawData(r.Day, r.TransactionType, r.CurrencyCode, r.Amount, r.Count))
            .ToList();

        var balanceData = await CalculateMonthBalanceAsync(userId, startDate, accountId, displayCurrency, cancellationToken);

        var allCurrencies = summaryData.Select(s => s.CurrencyCode)
            .Concat(topCategoriesData.Select(t => t.CurrencyCode))
            .Concat(dailyData.Select(d => d.CurrencyCode))
            .Distinct()
            .ToList();

        var exchangeRates = await GetExchangeRatesFromCollectionAsync(
            allCurrencies.Select(c => new { CurrencyCode = c }),
            x => x.CurrencyCode,
            displayCurrency,
            cancellationToken);

        return (summaryData, topCategoriesData, dailyData, balanceData, exchangeRates);
    }

    private List<CategorySummary> ProcessTopCategories(
        List<CategoryRawData> topCategoriesData,
        Dictionary<string, decimal> exchangeRates)
    {
        return topCategoriesData
            .GroupBy(t => t.CategoryName)
            .Select(g => new CategorySummary(
                g.Key,
                g.Sum(t => t.Amount * exchangeRates.GetValueOrDefault(t.CurrencyCode, 1m)),
                g.Sum(t => t.Count)
            ))
            .OrderByDescending(c => c.Amount)
            .Take(TopCategoriesLimit)
            .ToList();
    }

    private List<DailySummary> ProcessDailyBreakdown(
        List<DailyRawData> dailyData,
        Dictionary<string, decimal> exchangeRates)
    {
        return dailyData
            .GroupBy(d => d.Day)
            .Select(dayGroup =>
            {
                var (income, expense, incCount, expCount) = dayGroup.AggregateIncomeExpenseWithCount(
                    d => d.Amount,
                    d => d.CurrencyCode,
                    d => d.TransactionType,
                    d => d.Count,
                    exchangeRates);

                return new DailySummary(
                    Day: dayGroup.Key,
                    Income: Math.Round(income, 2),
                    Expense: Math.Round(expense, 2),
                    Net: Math.Round(income - expense, 2),
                    TransactionCount: incCount + expCount
                );
            })
            .OrderBy(d => d.Day)
            .ToList();
    }

    private async Task<(decimal StartingBalance, decimal EndingBalance)> CalculateMonthBalanceAsync(
        string userId,
        LocalDate monthStartDate,
        Guid? accountId,
        string displayCurrency,
        CancellationToken cancellationToken)
    {
        displayCurrency = displayCurrency.ToUpperInvariant();
        var monthEndDate = monthStartDate.PlusMonths(1).PlusDays(-1);

        var accounts = await accountBalanceService
            .GetAccountInitialBalancesAsync(userId, accountId, cancellationToken);

        if (accounts.Count == 0)
            return (0, 0);

        var accountIds = accounts.Select(a => a.AccountId).ToList();

        var previousTransactions = await transactionQueryService
            .GetTransactionTotalsBeforeDateAsync(accountIds, monthStartDate, cancellationToken);

        var monthTransactions = await transactionQueryService
            .GetTransactionTotalsInRangeAsync(accountIds, monthStartDate, monthEndDate, cancellationToken);

        var previousLookup = previousTransactions
            .GroupBy(t => t.AccountId)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    Income = g.FirstOrDefault(x => x.TransactionType == TransactionType.Income)?.Total ?? 0,
                    Expense = g.FirstOrDefault(x => x.TransactionType == TransactionType.Expense)?.Total ?? 0
                });

        var monthLookup = monthTransactions
            .GroupBy(t => t.AccountId)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    Income = g.FirstOrDefault(x => x.TransactionType == TransactionType.Income)?.Total ?? 0,
                    Expense = g.FirstOrDefault(x => x.TransactionType == TransactionType.Expense)?.Total ?? 0
                });

        var exchangeRates = await GetExchangeRatesFromCollectionAsync(
            accounts, a => a.CurrencyCode, displayCurrency, cancellationToken);

        var (initialBalance, previousIncome, previousExpense, monthIncome, monthExpense) = accounts
            .Aggregate(
                (Initial: 0m, PrevIncome: 0m, PrevExpense: 0m, MonthIncome: 0m, MonthExpense: 0m),
                (acc, a) =>
                {
                    var rate = exchangeRates.GetValueOrDefault(a.CurrencyCode, 1m);
                    var prevData = previousLookup.GetValueOrDefault(a.AccountId);
                    var monthData = monthLookup.GetValueOrDefault(a.AccountId);

                    return (
                        acc.Initial + a.InitialBalance * rate,
                        acc.PrevIncome + (prevData?.Income ?? 0) * rate,
                        acc.PrevExpense + (prevData?.Expense ?? 0) * rate,
                        acc.MonthIncome + (monthData?.Income ?? 0) * rate,
                        acc.MonthExpense + (monthData?.Expense ?? 0) * rate
                    );
                });

        var startingBalance = Math.Round(initialBalance + previousIncome - previousExpense, 2);
        var endingBalance = Math.Round(startingBalance + monthIncome - monthExpense, 2);

        return (startingBalance, endingBalance);
    }
}

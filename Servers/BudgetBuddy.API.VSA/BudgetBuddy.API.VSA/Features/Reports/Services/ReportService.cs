using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Common.Shared.Extensions;
using BudgetBuddy.API.VSA.Features.Reports.GetIncomeVsExpense;
using BudgetBuddy.API.VSA.Features.Reports.GetInvestmentPerformance;
using BudgetBuddy.API.VSA.Features.Reports.GetMonthlySummary;
using BudgetBuddy.API.VSA.Features.Reports.GetSpendingByCategory;
using System.Globalization;
using BudgetBuddy.API.VSA.Common.Infrastructure.Financial;

namespace BudgetBuddy.API.VSA.Features.Reports.Services;

public class ReportService(
    AppDbContext context,
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

        var query = context.Transactions.FilterByUser(userId, startDate, endDate, accountId);

        //TODO save at user if need speed up - or at least cache
        var groupedByCurrency = await query
            .AsNoTracking()
            .Where(t => t.CurrencyCode != null) 
            .GroupBy(t => new { t.CurrencyCode, t.TransactionType })
            .Select(g => new
            {
                CurrencyCode = g.Key.CurrencyCode.ToUpper(),
                TransactionType = g.Key.TransactionType,
                TotalAmount = g.Sum(t => t.Amount),
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);

        if (groupedByCurrency.Count == 0)
        {
            return (0, 0, 0, 0, 0);
        }

        var exchangeRates = await GetExchangeRatesFromCollectionAsync(
            groupedByCurrency,
            g => g.CurrencyCode,
            displayCurrency,
            cancellationToken);

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

        var query = context.Transactions.FilterByUser(userId, startDate, endDate, accountId);

        var groupedData = await query
            .AsNoTracking()
            .Where(t => t.CurrencyCode != null) 
            .GroupBy(t => new
            {
                t.TransactionDate.Year,
                t.TransactionDate.Month,
                t.CurrencyCode,
                t.TransactionType
            })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                CurrencyCode = g.Key.CurrencyCode.ToUpper(),
                g.Key.TransactionType,
                TotalAmount = g.Sum(t => t.Amount)
            })
            .ToListAsync(cancellationToken);

        if (groupedData.Count == 0)
        {
            return [];
        }

        var exchangeRates = await GetExchangeRatesFromCollectionAsync(
            groupedData,
            g => g.CurrencyCode,
            displayCurrency,
            cancellationToken);

        var monthlyData = groupedData
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

        return monthlyData;
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

        var query = context.Transactions
            .FilterByUser(userId, startDate, endDate, accountId)
            .Where(t => t.TransactionType == TransactionType.Expense);

        var groupedData = await query
            .AsNoTracking()
            .Where(t => t.CurrencyCode != null) // Filter nulls early
            .GroupBy(t => new
            {
                t.CategoryId,
                CategoryName = t.Category != null ? t.Category.Name : "Uncategorized",
                t.CurrencyCode
            })
            .Select(g => new
            {
                g.Key.CategoryId,
                g.Key.CategoryName,
                CurrencyCode = g.Key.CurrencyCode.ToUpper(),
                Amount = g.Sum(t => t.Amount),
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);

        if (groupedData.Count == 0)
        {
            return (0, new List<CategorySpending>());
        }

        var exchangeRates = await GetExchangeRatesFromCollectionAsync(
            groupedData,
            g => g.CurrencyCode,
            displayCurrency,
            cancellationToken);

        var categoryTotals = groupedData
            .GroupBy(g => new { g.CategoryId, g.CategoryName })
            .Select(catGroup => (
                CategoryId: catGroup.Key.CategoryId,
                CategoryName: catGroup.Key.CategoryName,
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

        // Load all data in parallel
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

        var query = context.Investments.FilterByUser(userId, startDate, endDate, type)
            .Where(i => i.SoldDate == null); // exclude sold positions from performance report

        // Load only needed fields with currency normalized
        var investments = await query
            .AsNoTracking()
            .Where(i => i.CurrencyCode != null) // Filter nulls early
            .Select(i => new
            {
                i.Id,
                i.Symbol,
                i.Name,
                i.Type,
                i.Quantity,
                i.PurchasePrice,
                i.PurchaseDate,
                CurrencyCode = i.CurrencyCode.ToUpperInvariant() // Normalize once
            })
            .ToListAsync(cancellationToken);

        if (!investments.Any())
        {
            return (0, 0, 0, 0, new List<InvestmentPerformance>());
        }

        var symbolsWithTypes = investments
            .Select(i => (i.Symbol, i.Type))
            .Distinct()
            .ToList();

        var exchangeRates = await GetExchangeRatesFromCollectionAsync(
            investments,
            i => i.CurrencyCode,
            displayCurrency,
            cancellationToken);

        // Create fallback prices (converted to display currency)
        // Group by Symbol to handle duplicate symbols (multiple investments in same stock)
        var purchasePriceFallback = investments
            .GroupBy(i => i.Symbol)
            .ToDictionary(
                g => g.Key,
                g => g.Average(i => i.PurchasePrice * exchangeRates.GetValueOrDefault(i.CurrencyCode, 1m)));

        // Fallback chain delegated to InvestmentCalculationService: live → snapshot → purchasePriceFallback
        var currentPrices = await investmentCalculationService.GetCurrentPricesAsync(
            symbolsWithTypes, displayCurrency, purchasePriceFallback, cancellationToken);

        var today = clock.GetCurrentInstant().InUtc().Date;

        // Load historical FX rates for cost basis (purchase date → display currency)
        // Falls back to today's live rate if no snapshot exists for a given date
        var minPurchaseDate = investments.Min(i => i.PurchaseDate);
        var historicalRates = await LoadHistoricalRatesAsync(minPurchaseDate, today, cancellationToken);

        var performanceList = investments.Select(i =>
        {
            // Cost basis: historical FX at purchase date
            var historicalRate = GetHistoricalCrossRate(
                historicalRates, i.CurrencyCode, displayCurrency, i.PurchaseDate,
                fallback: exchangeRates.GetValueOrDefault(i.CurrencyCode, 1m));
            var purchasePrice = i.PurchasePrice * historicalRate;

            // Current value: live price already in displayCurrency from IPriceService
            var currentPrice = currentPrices.GetValueOrDefault(i.Symbol, purchasePrice);
            var daysHeld = Period.Between(i.PurchaseDate, today, PeriodUnits.Days).Days;

            var (totalInvested, currentValue, gainLoss, gainLossPercentage) =
                investmentCalculationService.CalculateInvestmentMetrics(
                    i.Quantity,
                    purchasePrice,
                    currentPrice);

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

    /// <summary>
    /// Loads all ExchangeRateSnapshots from <paramref name="from"/> to <paramref name="to"/>
    /// and groups them by currency for fast date-based lookup.
    /// </summary>
    private async Task<Dictionary<string, List<ExchangeRateSnapshot>>> LoadHistoricalRatesAsync(
        LocalDate from, LocalDate to, CancellationToken cancellationToken)
    {
        var snapshots = await context.ExchangeRateSnapshots
            .Where(e => e.Date >= from && e.Date <= to)
            .OrderBy(e => e.Date)
            .ToListAsync(cancellationToken);

        return snapshots
            .GroupBy(e => e.Currency, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key.ToUpperInvariant(), g => g.ToList(), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns the cross-rate from <paramref name="fromCurrency"/> to <paramref name="toCurrency"/>
    /// using the most recent snapshot on or before <paramref name="date"/>.
    /// Formula: RateToUsd[from] / RateToUsd[to]
    /// Falls back to <paramref name="fallback"/> if no snapshot is found.
    /// </summary>
    private static decimal GetHistoricalCrossRate(
        Dictionary<string, List<ExchangeRateSnapshot>> historicalRates,
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

    private static decimal? GetLatestRateOnOrBefore(
        Dictionary<string, List<ExchangeRateSnapshot>> historicalRates,
        string currency,
        LocalDate date)
    {
        if (!historicalRates.TryGetValue(currency.ToUpperInvariant(), out var snapshots))
            return null;

        // Snapshots are ordered by date ascending; find the last one on or before the target date
        for (var i = snapshots.Count - 1; i >= 0; i--)
        {
            if (snapshots[i].Date <= date)
                return snapshots[i].RateToUsd;
        }

        return null;
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
        var query = context.Transactions
            .Where(t => t.UserId == userId &&
                       t.TransactionDate >= startDate &&
                       t.TransactionDate <= endDate);

        if (accountId.HasValue)
            query = query.Where(t => t.AccountId == accountId.Value);

        // Query 1: Summary with currency grouping
        var summaryData = await query
            .AsNoTracking()
            .Where(t => t.CurrencyCode != null)
            .GroupBy(t => new { t.TransactionType, t.CurrencyCode })
            .Select(g => new MonthlySummaryRawData(
                g.Key.TransactionType,
                g.Key.CurrencyCode.ToUpper(),
                g.Sum(t => t.Amount),
                g.Count()
            ))
            .ToListAsync(cancellationToken);

        // Query 2: Top expense categories with currency
        var topCategoriesData = await query
            .AsNoTracking()
            .Where(t => t.TransactionType == TransactionType.Expense && t.CurrencyCode != null)
            .GroupBy(t => new
            {
                CategoryName = t.Category != null ? t.Category.Name : "Uncategorized",
                t.CurrencyCode
            })
            .Select(g => new CategoryRawData(
                g.Key.CategoryName,
                g.Key.CurrencyCode.ToUpper(),
                g.Sum(t => t.Amount),
                g.Count()
            ))
            .ToListAsync(cancellationToken);

        // Query 3: Daily breakdown with currency and type grouping
        var dailyData = await query
            .AsNoTracking()
            .Where(t => t.CurrencyCode != null)
            .GroupBy(t => new { t.TransactionDate.Day, t.TransactionType, t.CurrencyCode })
            .Select(g => new DailyRawData(
                g.Key.Day,
                g.Key.TransactionType,
                g.Key.CurrencyCode.ToUpper(),
                g.Sum(t => t.Amount),
                g.Count()
            ))
            .ToListAsync(cancellationToken);

        // Query 4: Balance calculation
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

        var accountQuery = context.Accounts.Where(a => a.UserId == userId);
        if (accountId.HasValue)
            accountQuery = accountQuery.Where(a => a.Id == accountId.Value);

      
        var accounts = await accountQuery
            .AsNoTracking()
            .Select(a => new
            {
                a.Id,
                a.InitialBalance,
                CurrencyCode = a.DefaultCurrencyCode ?? "USD"
            })
            .ToListAsync(cancellationToken);

        if (accounts.Count == 0)
            return (0, 0);


        var accountIds = accounts.Select(a => a.Id).ToList();

        // Query 1: Transactions before the month
        var previousTransactions = await context.Transactions
            .AsNoTracking()
            .Where(t => accountIds.Contains(t.AccountId) && t.TransactionDate < monthStartDate)
            .GroupBy(t => new { t.AccountId, t.TransactionType })
            .Select(g => new
            {
                g.Key.AccountId,
                g.Key.TransactionType,
                Total = g.Sum(t => t.Amount)
            })
            .ToListAsync(cancellationToken);

        // Query 2: Transactions within the month
        var monthTransactions = await context.Transactions
            .AsNoTracking()
            .Where(t => accountIds.Contains(t.AccountId)
                && t.TransactionDate >= monthStartDate
                && t.TransactionDate <= monthEndDate)
            .GroupBy(t => new { t.AccountId, t.TransactionType })
            .Select(g => new
            {
                g.Key.AccountId,
                g.Key.TransactionType,
                Total = g.Sum(t => t.Amount)
            })
            .ToListAsync(cancellationToken);

        // Build lookup dictionaries for O(1) access
        // Optimized: DB already grouped by AccountId+TransactionType, just restructure
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
            accounts,
            a => a.CurrencyCode.ToUpperInvariant(),
            displayCurrency,
            cancellationToken);

        var (initialBalance, previousIncome, previousExpense, monthIncome, monthExpense) = accounts
            .Aggregate(
                (Initial: 0m, PrevIncome: 0m, PrevExpense: 0m, MonthIncome: 0m, MonthExpense: 0m),
                (acc, a) =>
                {
                    var rate = exchangeRates.GetValueOrDefault(a.CurrencyCode.ToUpperInvariant(), 1m);
                    var prevData = previousLookup.GetValueOrDefault(a.Id);
                    var monthData = monthLookup.GetValueOrDefault(a.Id);

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

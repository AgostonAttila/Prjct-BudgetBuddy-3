using BudgetBuddy.Module.Analytics.Features.Dashboard.GetDashboard;
using BudgetBuddy.Shared.Contracts.Budgets;
using BudgetBuddy.Shared.Contracts.ReferenceData;
using BudgetBuddy.Shared.Contracts.Transactions;
using BudgetBuddy.Shared.Infrastructure.Financial;
using Microsoft.Extensions.Caching.Hybrid;
using System.Globalization;

namespace BudgetBuddy.Module.Analytics.Features.Dashboard.Services;

public class DashboardService(
    ITransactionQueryService transactionQueryService,
    IBudgetQueryService budgetQueryService,
    ICategoryQueryService categoryQueryService,
    IInvestmentDataService investmentDataService,
    IAccountOwnershipService accountOwnershipService,
    HybridCache hybridCache,
    ICurrencyConversionService currencyConversionService,
    IInvestmentCalculationService investmentCalculationService,
    IAccountBalanceService accountBalanceService,
    ILogger<DashboardService> logger)
    : CurrencyServiceBase(currencyConversionService, logger), IDashboardService
{
    private const int DefaultTopCategoriesCount = 5;
    private const int DefaultRecentTransactionsCount = 10;

    public async Task<DashboardAccountsSummary> GetAccountsSummaryAsync(
        string userId,
        string displayCurrency = "USD",
        CancellationToken cancellationToken = default)
    {
        displayCurrency = displayCurrency.ToUpperInvariant();

        var cacheKey = $"dashboard:accounts-summary:{userId}:{displayCurrency}";

        return await hybridCache.GetOrCreateAsync(
            cacheKey,
            async cancel =>
            {
                var accountCount = await accountBalanceService.GetAccountCountAsync(userId, cancel);

                if (accountCount == 0)
                {
                    return new DashboardAccountsSummary(
                        TotalBalance: 0,
                        AccountCount: 0,
                        PrimaryCurrency: displayCurrency
                    );
                }

                var accountBalance = await accountBalanceService.CalculateTotalBalanceAsync(
                    userId, displayCurrency, upToDate: null, cancel);

                var investmentValue = await CalculateTotalInvestmentValueAsync(userId, displayCurrency, cancel);

                var totalBalance = Math.Round(accountBalance + investmentValue, 2);

                return new DashboardAccountsSummary(
                    TotalBalance: totalBalance,
                    AccountCount: accountCount,
                    PrimaryCurrency: displayCurrency
                );
            },
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(1)
            },
            tags: [$"user:{userId}"],
            cancellationToken: cancellationToken
        );
    }

    public async Task<DashboardMonthSummary> GetMonthSummaryAsync(
        string userId,
        int year,
        int month,
        string displayCurrency = "USD",
        CancellationToken cancellationToken = default)
    {
        displayCurrency = displayCurrency.ToUpperInvariant();

        var cacheKey = $"dashboard:month-summary:{userId}:{year}:{month}:{displayCurrency}";

        return await hybridCache.GetOrCreateAsync(
            cacheKey,
            async cancel =>
            {
                var monthStart = new LocalDate(year, month, 1);
                var monthEnd = monthStart.PlusMonths(1).PlusDays(-1);

                var rows = await transactionQueryService.GetMonthlyTotalsAsync(userId, monthStart, monthEnd, cancel);

                if (rows.Count == 0)
                {
                    return new DashboardMonthSummary(
                        Year: year,
                        Month: month,
                        MonthName: CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month),
                        TotalIncome: 0,
                        TotalExpense: 0,
                        NetIncome: 0,
                        TransactionCount: 0
                    );
                }

                var exchangeRates = await GetExchangeRatesFromCollectionAsync(
                    rows, r => r.CurrencyCode, displayCurrency, cancel);

                var (monthIncome, monthExpense) = rows.AggregateIncomeExpense(
                    r => r.TotalAmount,
                    r => r.CurrencyCode,
                    r => r.TransactionType,
                    exchangeRates);

                return new DashboardMonthSummary(
                    Year: year,
                    Month: month,
                    MonthName: CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month),
                    TotalIncome: Math.Round(monthIncome, 2),
                    TotalExpense: Math.Round(monthExpense, 2),
                    NetIncome: Math.Round(monthIncome - monthExpense, 2),
                    TransactionCount: rows.Sum(r => r.Count)
                );
            },
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(1)
            },
            tags: [$"user:{userId}"],
            cancellationToken: cancellationToken
        );
    }

    public async Task<DashboardBudgetSummary> GetBudgetSummaryAsync(
        string userId,
        int year,
        int month,
        string displayCurrency = "USD",
        CancellationToken cancellationToken = default)
    {
        displayCurrency = displayCurrency.ToUpperInvariant();

        var cacheKey = $"dashboard:budget-summary:{userId}:{year}:{month}:{displayCurrency}";

        return await hybridCache.GetOrCreateAsync(
            cacheKey,
            async cancel =>
            {
                var budgets = await budgetQueryService.GetBudgetsForMonthAsync(userId, year, month, cancel);

                if (budgets.Count == 0)
                {
                    return new DashboardBudgetSummary(
                        TotalBudget: 0,
                        TotalSpent: 0,
                        Remaining: 0,
                        UtilizationPercentage: 0,
                        OverBudgetCount: 0,
                        TotalBudgetCount: 0
                    );
                }

                var monthStart = new LocalDate(year, month, 1);
                var monthEnd = monthStart.PlusMonths(1).PlusDays(-1);

                var categoryIds = budgets.Select(b => b.CategoryId).ToList();
                var expenses = await transactionQueryService
                    .GetExpensesByCategoryIdsAsync(userId, monthStart, monthEnd, categoryIds, cancel);

                var allCurrencies = budgets.Select(b => b.CurrencyCode)
                    .Concat(expenses.Select(e => e.CurrencyCode))
                    .Distinct()
                    .ToList();

                var exchangeRates = await GetExchangeRatesAsync(allCurrencies, displayCurrency, cancel);

                var budgetsByCategoryId = budgets.ToDictionary(
                    b => b.CategoryId,
                    b => b.Amount * exchangeRates.GetValueOrDefault(b.CurrencyCode, 1m));

                var expensesByCategory = expenses
                    .GroupBy(e => e.CategoryId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(e => e.Total * exchangeRates.GetValueOrDefault(e.CurrencyCode, 1m)));

                var totalBudget = Math.Round(budgetsByCategoryId.Values.Sum(), 2);
                var totalSpent = 0m;
                var overBudgetCount = 0;

                foreach (var (categoryId, budgetAmount) in budgetsByCategoryId)
                {
                    var categorySpent = expensesByCategory.GetValueOrDefault(categoryId, 0);
                    totalSpent += categorySpent;
                    if (categorySpent > budgetAmount)
                        overBudgetCount++;
                }

                totalSpent = Math.Round(totalSpent, 2);

                return new DashboardBudgetSummary(
                    TotalBudget: totalBudget,
                    TotalSpent: totalSpent,
                    Remaining: totalBudget - totalSpent,
                    UtilizationPercentage: totalBudget > 0 ? Math.Round((totalSpent / totalBudget) * 100, 2) : 0,
                    OverBudgetCount: overBudgetCount,
                    TotalBudgetCount: budgets.Count
                );
            },
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(1)
            },
            tags: [$"user:{userId}"],
            cancellationToken: cancellationToken
        );
    }

    public async Task<IReadOnlyList<DashboardTopCategory>> GetTopCategoriesAsync(
        string userId,
        LocalDate startDate,
        LocalDate endDate,
        int topCount = DefaultTopCategoriesCount,
        string displayCurrency = "USD",
        CancellationToken cancellationToken = default)
    {
        displayCurrency = displayCurrency.ToUpperInvariant();

        var cacheKey = $"dashboard:top-categories:{userId}:{startDate}:{endDate}:{topCount}:{displayCurrency}";

        return await hybridCache.GetOrCreateAsync(
            cacheKey,
            async cancel =>
            {
                var expenses = await transactionQueryService
                    .GetExpensesGroupedByCategoryAsync(userId, startDate, endDate, cancel);

                if (expenses.Count == 0)
                    return (IReadOnlyList<DashboardTopCategory>)Array.Empty<DashboardTopCategory>();

                var exchangeRates = await GetExchangeRatesFromCollectionAsync(
                    expenses, e => e.CurrencyCode, displayCurrency, cancel);

                var categoryExpenses = expenses
                    .GroupBy(e => e.CategoryId)
                    .Select(g => new
                    {
                        CategoryId = g.Key,
                        Amount = Math.Round(g.Sum(e => e.Amount * exchangeRates.GetValueOrDefault(e.CurrencyCode, 1m)), 2),
                        Count = g.Sum(e => e.Count)
                    })
                    .OrderByDescending(x => x.Amount)
                    .Take(topCount)
                    .ToList();

                var totalExpense = categoryExpenses.Sum(c => c.Amount);

                var categoryIds = categoryExpenses
                    .Where(c => c.CategoryId.HasValue)
                    .Select(c => c.CategoryId!.Value)
                    .ToList();

                var categories = await categoryQueryService.GetCategoriesByIdsAsync(categoryIds, cancel);

                return (IReadOnlyList<DashboardTopCategory>)categoryExpenses
                    .Select(c =>
                    {
                        var catId = c.CategoryId ?? Guid.Empty;
                        categories.TryGetValue(catId, out var cat);
                        return new DashboardTopCategory(
                            CategoryId: catId,
                            CategoryName: cat?.Name ?? "Unknown",
                            CategoryIcon: cat?.Icon ?? "📁",
                            Amount: c.Amount,
                            TransactionCount: c.Count,
                            Percentage: totalExpense > 0 ? Math.Round((c.Amount / totalExpense) * 100, 2) : 0
                        );
                    })
                    .ToList();
            },
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(1)
            },
            tags: [$"user:{userId}"],
            cancellationToken: cancellationToken
        );
    }

    public async Task<IReadOnlyList<DashboardRecentTransaction>> GetRecentTransactionsAsync(
        string userId,
        int count = DefaultRecentTransactionsCount,
        CancellationToken cancellationToken = default)
    {
        var items = await transactionQueryService.GetRecentTransactionsAsync(userId, count, cancellationToken);

        if (items.Count == 0)
            return Array.Empty<DashboardRecentTransaction>();

        var accountIds = items.Select(t => t.AccountId).Distinct();
        var categoryIds = items.Where(t => t.CategoryId.HasValue).Select(t => t.CategoryId!.Value).Distinct();

        var accountNames = await accountOwnershipService.GetAccountNamesByIdsAsync(accountIds, cancellationToken);
        var categories = await categoryQueryService.GetCategoriesByIdsAsync(categoryIds, cancellationToken);

        return items.Select(t =>
        {
            categories.TryGetValue(t.CategoryId ?? Guid.Empty, out var cat);
            return new DashboardRecentTransaction(
                t.Id,
                t.TransactionDate.ToString("yyyy-MM-dd", null),
                accountNames.GetValueOrDefault(t.AccountId, t.AccountId.ToString()),
                cat?.Name ?? "Uncategorized",
                t.TransactionType.ToString(),
                t.Amount,
                t.CurrencyCode
            );
        }).ToList();
    }

    private async Task<decimal> CalculateTotalInvestmentValueAsync(
        string userId, string targetCurrency, CancellationToken cancellationToken)
    {
        var investments = await investmentDataService.GetActiveInvestmentsAsync(
            userId, null, null, null, cancellationToken);

        if (investments.Count == 0)
            return 0m;

        var consolidated = investments
            .GroupBy(i => i.Symbol)
            .Select(g => new
            {
                Symbol = g.Key,
                Type = g.First().Type,
                TotalQuantity = g.Sum(i => i.Quantity),
                WeightedAvgPurchasePrice = g.Sum(i => i.Quantity * i.PurchasePrice) / g.Sum(i => i.Quantity)
            })
            .ToList();

        var symbolsWithTypes = consolidated.Select(i => (i.Symbol, i.Type)).ToList();
        var purchasePriceFallback = consolidated.ToDictionary(i => i.Symbol, i => i.WeightedAvgPurchasePrice);

        var currentPrices = await investmentCalculationService.GetCurrentPricesAsync(
            symbolsWithTypes, targetCurrency, purchasePriceFallback, cancellationToken);

        var total = consolidated.Sum(inv =>
        {
            var price = currentPrices.GetValueOrDefault(inv.Symbol, inv.WeightedAvgPurchasePrice);
            var (_, currentValue, _, _) = investmentCalculationService.CalculateInvestmentMetrics(
                inv.TotalQuantity, inv.WeightedAvgPurchasePrice, price);
            return currentValue;
        });

        return Math.Round(total, 2);
    }
}

using BudgetBuddy.Application.Common.Contracts;
using BudgetBuddy.Application.Common.Extensions;
using BudgetBuddy.Application.Features.Dashboard.GetDashboard;
using BudgetBuddy.Application.Features.Dashboard.Services;
using Microsoft.Extensions.Caching.Hybrid;
using System.Globalization;
using BudgetBuddy.Infrastructure.Financial;

namespace BudgetBuddy.Infrastructure.Services;

// TODO: Performance Optimization - Parallel Dashboard Query Execution
// Currently, all dashboard queries run sequentially to avoid DbContext concurrency issues.
// To enable parallel execution (5x faster dashboard loading):
//
// 1. Add DbContextFactory registration in DatabaseExtensions.cs:
//    services.AddDbContextFactory<AppDbContext>(...);
//
// 2. Inject IDbContextFactory<AppDbContext> instead of AppDbContext:
//    public class DashboardService(IDbContextFactory<AppDbContext> contextFactory, ...)
//
// 3. Create separate DbContext instances in each method:
//    using var context = _contextFactory.CreateDbContext();
//
// 4. Update GetDashboardHandler to use Task.WhenAll for parallel execution
//
// CHALLENGE: Current setup uses scoped interceptors (RLS, AuditLog) that depend on HttpContext.
// SOLUTION OPTIONS:
//   A) Simple: AddDbContextFactory with same interceptors (may have scope issues)
//   B) Optimal: Create ReadOnlyAppDbContext without AuditLog interceptor (read-only queries don't need audit)
//
// EXPECTED PERFORMANCE GAIN: ~5 sequential queries (250ms) → 1 parallel batch (~50-100ms)
// HYBRID CACHE: L1 (in-memory) + L2 (Redis) for ultra-fast dashboard responses
public class DashboardService(
    AppDbContext context,
    HybridCache hybridCache,
    CurrencyConverter currencyConverter,
    IInvestmentCalculationService investmentCalculationService,
    IAccountBalanceService accountBalanceService,
    ILogger<DashboardService> logger) : IDashboardService
{
    private const int DefaultTopCategoriesCount = 5;
    private const int DefaultRecentTransactionsCount = 10;
    public async Task<DashboardAccountsSummary> GetAccountsSummaryAsync(
        string userId,
        string displayCurrency = "USD",
        CancellationToken cancellationToken = default)
    {
        displayCurrency = displayCurrency.ToUpperInvariant();

        // Hybrid cache key: user-specific + currency-specific
        var cacheKey = $"dashboard:accounts-summary:{userId}:{displayCurrency}";

        return await hybridCache.GetOrCreateAsync(
            cacheKey,
            async cancel =>
            {
                var accountCount = await context.Accounts
                    .AsNoTracking()
                    .Where(a => a.UserId == userId)
                    .CountAsync(cancel);

                if (accountCount == 0)
                {
                    return new DashboardAccountsSummary(
                        TotalBalance: 0,
                        AccountCount: 0,
                        PrimaryCurrency: displayCurrency
                    );
                }

                var accountBalance = await accountBalanceService.CalculateTotalBalanceAsync(
                    userId,
                    displayCurrency,
                    upToDate: null,
                    cancel);

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
                Expiration = TimeSpan.FromMinutes(5),       // L2 cache (Redis): 5 minutes
                LocalCacheExpiration = TimeSpan.FromMinutes(1) // L1 cache (memory): 1 minute
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

                var transactions = await context.Transactions
                    .FilterByUser(userId, monthStart, monthEnd)
                    .AsNoTracking()
                    .Where(t => t.CurrencyCode != null)
                    .Select(t => new
                    {
                        t.TransactionType,
                        t.Amount,
                        CurrencyCode = t.CurrencyCode.ToUpper()
                    })
                    .ToListAsync(cancel);

                if (transactions.Count == 0)
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

                var exchangeRates = await currencyConverter.GetExchangeRatesFromCollectionAsync(
                    transactions,
                    t => t.CurrencyCode,
                    displayCurrency,
                    cancel);

                var (monthIncome, monthExpense) = transactions.AggregateIncomeExpense(
                    t => t.Amount,
                    t => t.CurrencyCode,
                    t => t.TransactionType,
                    exchangeRates);

                return new DashboardMonthSummary(
                    Year: year,
                    Month: month,
                    MonthName: CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month),
                    TotalIncome: Math.Round(monthIncome, 2),
                    TotalExpense: Math.Round(monthExpense, 2),
                    NetIncome: Math.Round(monthIncome - monthExpense, 2),
                    TransactionCount: transactions.Count
                );
            },
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),       // L2 cache (Redis): 5 minutes
                LocalCacheExpiration = TimeSpan.FromMinutes(1) // L1 cache (memory): 1 minute
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
                var budgets = await context.Budgets
                    .AsNoTracking()
                    .Where(b => b.UserId == userId && b.Year == year && b.Month == month)
                    .Select(b => new
                    {
                        b.CategoryId,
                        b.Amount,
                        CurrencyCode = (b.CurrencyCode ?? "USD").ToUpper()
                    })
                    .ToListAsync(cancel);

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
                var expenses = await context.Transactions
                    .AsNoTracking()
                    .Where(t => t.UserId == userId &&
                               t.TransactionDate >= monthStart &&
                               t.TransactionDate <= monthEnd &&
                               t.TransactionType == TransactionType.Expense &&
                               t.CategoryId.HasValue &&
                               t.CurrencyCode != null &&
                               categoryIds.Contains(t.CategoryId.Value))
                    .Select(e => new
                    {
                        e.CategoryId,
                        e.Amount,
                        CurrencyCode = e.CurrencyCode.ToUpper()
                    })
                    .ToListAsync(cancel);

                var allCurrencies = budgets.Select(b => b.CurrencyCode)
                    .Concat(expenses.Select(e => e.CurrencyCode))
                    .Distinct()
                    .ToList();

                var exchangeRates = await currencyConverter.GetExchangeRatesAsync(
                    allCurrencies,
                    displayCurrency,
                    cancel);

                var budgetsByCategoryId = budgets.ToDictionary(
                    b => b.CategoryId,
                    b => b.Amount * exchangeRates.GetValueOrDefault(b.CurrencyCode, 1m));

                var expensesByCategory = expenses
                    .GroupBy(e => e.CategoryId!.Value)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(e => e.Amount * exchangeRates.GetValueOrDefault(e.CurrencyCode, 1m)));

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
                var expenses = await context.Transactions
                    .FilterByUser(userId, startDate, endDate)
                    .AsNoTracking()
                    .Where(t => t.TransactionType == TransactionType.Expense &&
                               t.CategoryId.HasValue &&
                               t.CurrencyCode != null)
                    .Select(t => new
                    {
                        t.CategoryId,
                        t.Amount,
                        CurrencyCode = t.CurrencyCode.ToUpper()
                    })
                    .ToListAsync(cancel);

                if (expenses.Count == 0)
                    return (IReadOnlyList<DashboardTopCategory>)Array.Empty<DashboardTopCategory>();

                var exchangeRates = await currencyConverter.GetExchangeRatesFromCollectionAsync(
                    expenses,
                    e => e.CurrencyCode,
                    displayCurrency,
                    cancel);

                var categoryExpenses = expenses
                    .GroupBy(e => e.CategoryId!.Value)
                    .Select(g => new
                    {
                        CategoryId = g.Key,
                        Amount = Math.Round(g.Sum(e => e.Amount * exchangeRates.GetValueOrDefault(e.CurrencyCode, 1m)), 2),
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Amount)
                    .Take(topCount)
                    .ToList();

                var totalExpense = categoryExpenses.Sum(c => c.Amount);

                var categoryIds = categoryExpenses.Select(c => c.CategoryId).ToList();
                var categories = await context.Categories
                    .AsNoTracking()
                    .Where(c => categoryIds.Contains(c.Id))
                    .Select(c => new { c.Id, c.Name, c.Icon })
                    .ToDictionaryAsync(c => c.Id, cancel);

                return (IReadOnlyList<DashboardTopCategory>)categoryExpenses
                    .Select(c => new DashboardTopCategory(
                        CategoryId: c.CategoryId,
                        CategoryName: categories.ContainsKey(c.CategoryId) ? categories[c.CategoryId].Name : "Unknown",
                        CategoryIcon: categories.ContainsKey(c.CategoryId) ? categories[c.CategoryId].Icon ?? "📁" : "📁",
                        Amount: c.Amount,
                        TransactionCount: c.Count,
                        Percentage: totalExpense > 0 ? Math.Round((c.Amount / totalExpense) * 100, 2) : 0
                    ))
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

        return await context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.CreatedAt)
            .Take(count)
            .Select(t => new DashboardRecentTransaction(
                t.Id,
                t.TransactionDate.ToString("yyyy-MM-dd", null),
                t.Account.Name ?? "Unknown",
                t.Category.Name ?? "Uncategorized",
                t.TransactionType.ToString(),
                t.Amount,
                t.CurrencyCode
                // Note and Payee excluded for security (PII data not cached)
            ))
            .ToListAsync(cancellationToken);

        // Note: recent transactions are intentionally not cached — contain PII (Note, Payee)
    }

    private async Task<decimal> CalculateTotalInvestmentValueAsync(
        string userId, string targetCurrency, CancellationToken cancellationToken)
    {
        var investments = await context.Investments
            .AsNoTracking()
            .Where(i => i.UserId == userId && i.SoldDate == null)
            .Select(i => new { i.Symbol, i.Type, i.Quantity, i.PurchasePrice })
            .ToListAsync(cancellationToken);

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

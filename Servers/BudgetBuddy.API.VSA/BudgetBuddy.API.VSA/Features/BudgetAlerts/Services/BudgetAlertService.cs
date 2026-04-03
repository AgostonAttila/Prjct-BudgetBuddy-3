using BudgetBuddy.API.VSA.Common.Infrastructure.Financial;
using BudgetBuddy.API.VSA.Common.Shared.Services;
using BudgetBuddy.API.VSA.Features.BudgetAlerts.GetBudgetAlerts;

namespace BudgetBuddy.API.VSA.Features.BudgetAlerts.Services;

public class BudgetAlertService(
    AppDbContext context,
    ICurrencyConversionService currencyConversionService,
    ILogger<BudgetAlertService> logger) : CurrencyServiceBase(currencyConversionService, logger), IBudgetAlertService
{
    public async Task<IReadOnlyList<BudgetAlertDto>> CalculateAlertsAsync(
        string userId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var budgets = await GetBudgetsForMonthAsync(userId, year, month, cancellationToken);

        if (budgets.Count == 0)
            return [];

        var rawSpending = await GetCategorySpendingRawAsync(userId, year, month, cancellationToken);

        // Pre-fetch exchange rates per unique budget currency.
        // CurrencyServiceBase caches rates per target currency, so repeated calls are cheap.
        var budgetCurrencies = budgets
            .Select(b => b.CurrencyCode.ToUpperInvariant())
            .Distinct()
            .ToList();

        var ratesByBudgetCurrency = new Dictionary<string, Dictionary<string, decimal>>(StringComparer.OrdinalIgnoreCase);
        foreach (var budgetCurrency in budgetCurrencies)
        {
            ratesByBudgetCurrency[budgetCurrency] = await GetExchangeRatesFromCollectionAsync(
                rawSpending, r => r.CurrencyCode, budgetCurrency, cancellationToken);
        }

        return budgets
            .Select(budget =>
            {
                var budgetCurrency = budget.CurrencyCode.ToUpperInvariant();
                var rates = ratesByBudgetCurrency[budgetCurrency];

                var actualAmount = Math.Round(
                    rawSpending
                        .Where(r => r.CategoryId == budget.CategoryId)
                        .Sum(r => r.Total * rates.GetValueOrDefault(r.CurrencyCode, 1m)),
                    2);

                var remaining = budget.Amount - actualAmount;
                var utilizationPercentage = CalculateUtilizationPercentage(budget.Amount, actualAmount);
                var alertLevel = DetermineAlertLevel(utilizationPercentage);
                var message = GenerateAlertMessage(alertLevel, utilizationPercentage, remaining);

                return new BudgetAlertDto(
                    BudgetId: budget.Id,
                    CategoryId: budget.CategoryId,
                    CategoryName: budget.Category?.Name ?? "Unknown",
                    CategoryIcon: budget.Category?.Icon ?? "📁",
                    BudgetAmount: budget.Amount,
                    ActualAmount: actualAmount,
                    Remaining: remaining,
                    UtilizationPercentage: utilizationPercentage,
                    AlertLevel: alertLevel,
                    Message: message
                );
            })
            .Where(alert => alert.AlertLevel != AlertLevel.Safe)
            .OrderByDescending(a => a.UtilizationPercentage)
            .ToList();
    }

    public decimal CalculateCategorySpending(
        IEnumerable<Transaction> transactions,
        Guid categoryId)
    {
        return transactions
            .Where(t => t.CategoryId == categoryId)
            .Sum(t => t.Amount);
    }

    public AlertLevel DetermineAlertLevel(decimal utilizationPercentage)
    {
        return utilizationPercentage switch
        {
            >= 100 => AlertLevel.Exceeded,
            >= 80 => AlertLevel.Warning,
            _ => AlertLevel.Safe
        };
    }

    public string GenerateAlertMessage(
        AlertLevel alertLevel,
        decimal utilizationPercentage,
        decimal remaining)
    {
        return alertLevel switch
        {
            AlertLevel.Warning =>
                $"⚠️ You've used {utilizationPercentage}% of your budget. {Math.Abs(remaining):F2} remaining.",
            AlertLevel.Exceeded =>
                $"🚨 Budget exceeded by {Math.Abs(remaining):F2}! You've spent {utilizationPercentage}% of your budget.",
            _ => string.Empty
        };
    }

    private async Task<IReadOnlyList<Budget>> GetBudgetsForMonthAsync(
        string userId,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        return await context.Budgets
            .AsNoTracking()
            .Include(b => b.Category)
            .Where(b => b.UserId == userId && b.Year == year && b.Month == month)
            .ToListAsync(cancellationToken);
    }

    private async Task<List<SpendingRow>> GetCategorySpendingRawAsync(
        string userId,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        var monthStart = new LocalDate(year, month, 1);
        var monthEnd = monthStart.PlusMonths(1).PlusDays(-1);

        var rows = await context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId &&
                        t.TransactionDate >= monthStart &&
                        t.TransactionDate <= monthEnd &&
                        t.TransactionType == TransactionType.Expense &&
                        t.CategoryId.HasValue &&
                        t.CurrencyCode != null)
            .GroupBy(t => new { t.CategoryId, t.CurrencyCode })
            .Select(g => new
            {
                CategoryId = g.Key.CategoryId!.Value,
                CurrencyCode = g.Key.CurrencyCode.ToUpper(),
                Total = g.Sum(t => t.Amount)
            })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new SpendingRow(r.CategoryId, r.CurrencyCode, r.Total)).ToList();
    }

    private static decimal CalculateUtilizationPercentage(decimal budgetAmount, decimal actualAmount)
    {
        return budgetAmount > 0
            ? Math.Round((actualAmount / budgetAmount) * 100, 2)
            : 0;
    }

    private record SpendingRow(Guid CategoryId, string CurrencyCode, decimal Total);
}

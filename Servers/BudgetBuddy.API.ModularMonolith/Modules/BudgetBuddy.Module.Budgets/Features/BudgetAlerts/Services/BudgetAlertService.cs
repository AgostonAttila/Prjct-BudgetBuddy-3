using BudgetBuddy.Shared.Contracts.ReferenceData;
using BudgetBuddy.Shared.Contracts.Transactions;
using BudgetBuddy.Shared.Infrastructure.Financial;
using BudgetBuddy.Shared.Infrastructure.Services;
using BudgetBuddy.Module.Budgets.Features.BudgetAlerts.GetBudgetAlerts;

namespace BudgetBuddy.Module.Budgets.Features.BudgetAlerts.Services;

public class BudgetAlertService(
    BudgetsDbContext context,
    ITransactionQueryService transactionQueryService,
    ICategoryQueryService categoryQueryService,
    ICurrencyConversionService currencyConversionService,
    ILogger<BudgetAlertService> logger) : CurrencyServiceBase(currencyConversionService, logger), IBudgetAlertService
{
    public async Task<IReadOnlyList<BudgetAlertDto>> CalculateAlertsAsync(
        string userId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var budgets = await context.Budgets
            .AsNoTracking()
            .Where(b => b.UserId == userId && b.Year == year && b.Month == month)
            .ToListAsync(cancellationToken);

        if (budgets.Count == 0)
            return [];

        var categoryIds = budgets.Select(b => b.CategoryId).Distinct().ToList();
        var categories = await categoryQueryService.GetCategoriesByIdsAsync(categoryIds, cancellationToken);

        var monthStart = new LocalDate(year, month, 1);
        var monthEnd = monthStart.PlusMonths(1).PlusDays(-1);
        var rawSpending = await transactionQueryService
            .GetCategorySpendingAsync(userId, monthStart, monthEnd, cancellationToken);

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
                categories.TryGetValue(budget.CategoryId, out var category);

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
                    CategoryName: category?.Name ?? "Unknown",
                    CategoryIcon: category?.Icon ?? "📁",
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

    private static decimal CalculateUtilizationPercentage(decimal budgetAmount, decimal actualAmount)
    {
        return budgetAmount > 0
            ? Math.Round((actualAmount / budgetAmount) * 100, 2)
            : 0;
    }
}

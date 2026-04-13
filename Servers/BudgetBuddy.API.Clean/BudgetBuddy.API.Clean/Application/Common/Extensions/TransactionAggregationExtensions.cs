using BudgetBuddy.Domain.Enums;

namespace BudgetBuddy.Application.Common.Extensions;

/// <summary>
/// Extension methods for aggregating transaction data with currency conversion
/// </summary>
public static class TransactionAggregationExtensions
{
    /// <summary>
    /// Aggregates income and expense from a collection with currency conversion
    /// </summary>
    /// <typeparam name="T">Type of items to aggregate</typeparam>
    /// <param name="items">Collection to aggregate</param>
    /// <param name="amountSelector">Function to extract amount from item</param>
    /// <param name="currencyCodeSelector">Function to extract currency code from item</param>
    /// <param name="transactionTypeSelector">Function to extract transaction type from item</param>
    /// <param name="exchangeRates">Dictionary of exchange rates by currency code</param>
    /// <returns>Tuple of (TotalIncome, TotalExpense)</returns>
    public static (decimal TotalIncome, decimal TotalExpense) AggregateIncomeExpense<T>(
        this IEnumerable<T> items,
        Func<T, decimal> amountSelector,
        Func<T, string> currencyCodeSelector,
        Func<T, TransactionType> transactionTypeSelector,
        IReadOnlyDictionary<string, decimal> exchangeRates)
    {
        return items.Aggregate(
            (Income: 0m, Expense: 0m),
            (acc, item) =>
            {
                var amount = amountSelector(item);
                var currencyCode = currencyCodeSelector(item);
                var rate = exchangeRates.GetValueOrDefault(currencyCode, 1m);
                var convertedAmount = amount * rate;

                var type = transactionTypeSelector(item);
                if (type == TransactionType.Transfer) return acc;
                return type == TransactionType.Income
                    ? (acc.Income + convertedAmount, acc.Expense)
                    : (acc.Income, acc.Expense + convertedAmount);
            });
    }

    /// <summary>
    /// Aggregates income, expense, and count from a collection with currency conversion
    /// </summary>
    /// <typeparam name="T">Type of items to aggregate</typeparam>
    /// <param name="items">Collection to aggregate</param>
    /// <param name="amountSelector">Function to extract amount from item</param>
    /// <param name="currencyCodeSelector">Function to extract currency code from item</param>
    /// <param name="transactionTypeSelector">Function to extract transaction type from item</param>
    /// <param name="countSelector">Function to extract count from item</param>
    /// <param name="exchangeRates">Dictionary of exchange rates by currency code</param>
    /// <returns>Tuple of (TotalIncome, TotalExpense, IncomeCount, ExpenseCount)</returns>
    public static (decimal TotalIncome, decimal TotalExpense, int IncomeCount, int ExpenseCount) AggregateIncomeExpenseWithCount<T>(
        this IEnumerable<T> items,
        Func<T, decimal> amountSelector,
        Func<T, string> currencyCodeSelector,
        Func<T, TransactionType> transactionTypeSelector,
        Func<T, int> countSelector,
        IReadOnlyDictionary<string, decimal> exchangeRates)
    {
        return items.Aggregate(
            (Income: 0m, Expense: 0m, IncomeCount: 0, ExpenseCount: 0),
            (acc, item) =>
            {
                var amount = amountSelector(item);
                var currencyCode = currencyCodeSelector(item);
                var rate = exchangeRates.GetValueOrDefault(currencyCode, 1m);
                var convertedAmount = amount * rate;
                var count = countSelector(item);

                var type = transactionTypeSelector(item);
                if (type == TransactionType.Transfer) return acc;
                return type == TransactionType.Income
                    ? (acc.Income + convertedAmount, acc.Expense, acc.IncomeCount + count, acc.ExpenseCount)
                    : (acc.Income, acc.Expense + convertedAmount, acc.IncomeCount, acc.ExpenseCount + count);
            });
    }
}

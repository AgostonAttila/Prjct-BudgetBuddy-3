namespace BudgetBuddy.Module.Analytics.Extensions;

public static class TransactionAggregationExtensions
{
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

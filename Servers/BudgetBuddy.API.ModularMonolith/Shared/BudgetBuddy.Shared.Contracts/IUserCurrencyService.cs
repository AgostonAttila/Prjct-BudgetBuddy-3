namespace BudgetBuddy.Shared.Contracts;

public interface IUserCurrencyService
{
    /// <summary>
    /// Returns the requested currency if provided, otherwise falls back to the current user's DefaultCurrency, then "USD".
    /// </summary>
    Task<string> GetDisplayCurrencyAsync(string? requestedCurrency, CancellationToken cancellationToken = default);
}

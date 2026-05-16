using BudgetBuddy.Shared.Messages.Contracts;

namespace BudgetBuddy.Service.Investments.ReadModels;

/// <summary>
/// Investments-local implementation: returns the requested currency or "USD" as fallback.
/// </summary>
public class UserCurrencyService : IUserCurrencyService
{
    public Task<string> GetDisplayCurrencyAsync(
        string? requestedCurrency,
        CancellationToken cancellationToken = default)
        => Task.FromResult(
            string.IsNullOrWhiteSpace(requestedCurrency) ? "USD" : requestedCurrency.ToUpperInvariant());
}

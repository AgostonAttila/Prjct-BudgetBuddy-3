using BudgetBuddy.Shared.Messages.Contracts;

namespace BudgetBuddy.Service.Analytics.ReadModels;

/// <summary>
/// Analytics-local implementation: returns the requested currency, or "USD" as fallback.
/// User default currency preference is stored in UserSettings (ReferenceData service)
/// and is not replicated to Analytics in this phase.
/// </summary>
public class UserCurrencyService : IUserCurrencyService
{
    public Task<string> GetDisplayCurrencyAsync(
        string? requestedCurrency,
        CancellationToken cancellationToken = default)
        => Task.FromResult(
            string.IsNullOrWhiteSpace(requestedCurrency) ? "USD" : requestedCurrency.ToUpperInvariant());
}

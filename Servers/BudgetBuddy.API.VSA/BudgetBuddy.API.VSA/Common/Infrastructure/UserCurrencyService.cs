using BudgetBuddy.API.VSA.Common.Shared.Contracts;

namespace BudgetBuddy.API.VSA.Common.Infrastructure;

public class UserCurrencyService(
    ICurrentUserService currentUserService,
    AppDbContext context) : IUserCurrencyService
{
    public async Task<string> GetDisplayCurrencyAsync(string? requestedCurrency, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(requestedCurrency))
            return requestedCurrency.ToUpperInvariant();

        var userId = currentUserService.GetCurrentUserId();

        var userCurrency = await context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.DefaultCurrency)
            .FirstOrDefaultAsync(cancellationToken);

        return userCurrency ?? "USD";
    }
}

using Microsoft.AspNetCore.Identity;

namespace BudgetBuddy.Module.Auth.Services;

public class UserCurrencyService(
    ICurrentUserService currentUserService,
    UserManager<User> userManager) : IUserCurrencyService
{
    public async Task<string> GetDisplayCurrencyAsync(string? requestedCurrency, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(requestedCurrency))
            return requestedCurrency.ToUpperInvariant();

        var userId = currentUserService.GetCurrentUserId();
        if (userId is null)
            return "USD";

        var user = await userManager.FindByIdAsync(userId);
        return user?.DefaultCurrency ?? "USD";
    }
}

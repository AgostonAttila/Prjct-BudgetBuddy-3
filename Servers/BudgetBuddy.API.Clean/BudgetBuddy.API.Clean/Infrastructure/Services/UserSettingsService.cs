using BudgetBuddy.Application.Features.UserSettings.Services;
using Microsoft.AspNetCore.Identity;

namespace BudgetBuddy.Infrastructure.Services;

public class UserSettingsService(
    UserManager<User> userManager,
    ILogger<UserSettingsService> logger) : IUserSettingsService
{
    public async Task<UserSettingsData> GetAsync(string userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException(nameof(User), userId);

        return new UserSettingsData(
            UserId: user.Id,
            Email: user.Email ?? "",
            FirstName: user.FirstName,
            LastName: user.LastName,
            PreferredLanguage: user.PreferredLanguage,
            DefaultCurrency: user.DefaultCurrency,
            DateFormat: user.DateFormat);
    }

    public async Task<UserSettingsUpdateResult> UpdateAsync(
        string userId, UserSettingsUpdateData update, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException(nameof(User), userId);

        if (!string.IsNullOrWhiteSpace(update.FirstName))
            user.FirstName = update.FirstName;

        if (!string.IsNullOrWhiteSpace(update.LastName))
            user.LastName = update.LastName;

        if (!string.IsNullOrWhiteSpace(update.PreferredLanguage))
            user.PreferredLanguage = update.PreferredLanguage;

        if (!string.IsNullOrWhiteSpace(update.DefaultCurrency))
            user.DefaultCurrency = update.DefaultCurrency;

        if (!string.IsNullOrWhiteSpace(update.DateFormat))
            user.DateFormat = update.DateFormat;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new DomainValidationException($"Failed to update user settings: {errors}");
        }

        logger.LogInformation("User settings updated for user {UserId}", userId);

        return new UserSettingsUpdateResult(
            PreferredLanguage: user.PreferredLanguage,
            DefaultCurrency: user.DefaultCurrency,
            DateFormat: user.DateFormat);
    }
}

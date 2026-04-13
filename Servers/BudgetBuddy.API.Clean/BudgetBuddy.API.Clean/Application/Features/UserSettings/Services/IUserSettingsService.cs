namespace BudgetBuddy.Application.Features.UserSettings.Services;

public record UserSettingsData(
    string UserId,
    string Email,
    string? FirstName,
    string? LastName,
    string PreferredLanguage,
    string DefaultCurrency,
    string DateFormat);

public record UserSettingsUpdateData(
    string? FirstName,
    string? LastName,
    string? PreferredLanguage,
    string? DefaultCurrency,
    string? DateFormat);

public record UserSettingsUpdateResult(
    string PreferredLanguage,
    string DefaultCurrency,
    string DateFormat);

public interface IUserSettingsService
{
    Task<UserSettingsData> GetAsync(string userId, CancellationToken ct = default);
    Task<UserSettingsUpdateResult> UpdateAsync(string userId, UserSettingsUpdateData update, CancellationToken ct = default);
}

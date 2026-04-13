namespace BudgetBuddy.Application.Features.UserSettings.GetUserSettings;

public record GetUserSettingsQuery : IRequest<GetUserSettingsResponse>;

public record GetUserSettingsResponse(
    string UserId,
    string Email,
    string? FirstName,
    string? LastName,
    string PreferredLanguage,
    string DefaultCurrency,
    string DateFormat
);

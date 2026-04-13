namespace BudgetBuddy.Application.Features.UserSettings.UpdateUserSettings;

public record UpdateUserSettingsCommand(
    string? FirstName,
    string? LastName,
    string? PreferredLanguage,
    string? DefaultCurrency,
    string? DateFormat
) : IRequest<UpdateUserSettingsResponse>;

public record UpdateUserSettingsResponse(
    string Message,
    string PreferredLanguage,
    string DefaultCurrency,
    string DateFormat
);

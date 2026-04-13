using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Features.UserSettings.Services;

namespace BudgetBuddy.Application.Features.UserSettings.UpdateUserSettings;

public class UpdateUserSettingsHandler(
    IUserSettingsService userSettingsService,
    ICurrentUserService currentUserService,
    ILogger<UpdateUserSettingsHandler> logger)
    : UserAwareHandler<UpdateUserSettingsCommand, UpdateUserSettingsResponse>(currentUserService)
{
    public override async Task<UpdateUserSettingsResponse> Handle(
        UpdateUserSettingsCommand request,
        CancellationToken cancellationToken)
    {
        var update = new UserSettingsUpdateData(
            FirstName: request.FirstName,
            LastName: request.LastName,
            PreferredLanguage: request.PreferredLanguage,
            DefaultCurrency: request.DefaultCurrency,
            DateFormat: request.DateFormat);

        var result = await userSettingsService.UpdateAsync(UserId, update, cancellationToken);

        logger.LogInformation("User settings updated for user {UserId}", UserId);

        return new UpdateUserSettingsResponse(
            Message: "User settings updated successfully",
            PreferredLanguage: result.PreferredLanguage,
            DefaultCurrency: result.DefaultCurrency,
            DateFormat: result.DateFormat);
    }
}

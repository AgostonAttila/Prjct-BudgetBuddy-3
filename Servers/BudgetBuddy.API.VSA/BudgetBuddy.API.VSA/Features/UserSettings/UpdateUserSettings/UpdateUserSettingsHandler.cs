using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Shared.Handlers;
using Microsoft.AspNetCore.Identity;

namespace BudgetBuddy.API.VSA.Features.UserSettings.UpdateUserSettings;

public class UpdateUserSettingsHandler(
    UserManager<User> userManager,
    ICurrentUserService currentUserService,
    ILogger<UpdateUserSettingsHandler> logger) : UserAwareHandler<UpdateUserSettingsCommand, UpdateUserSettingsResponse>(currentUserService)
{
    public override async Task<UpdateUserSettingsResponse> Handle(
        UpdateUserSettingsCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(UserId)
            ?? throw new NotFoundException(nameof(User), UserId);

        SetUserProperties(request, user);

        var result = await userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new ValidationException($"Failed to update user settings: {errors}");
        }

        logger.LogInformation("User settings updated for user {UserId}", UserId);

        return new UpdateUserSettingsResponse(
            Message: "User settings updated successfully",
            PreferredLanguage: user.PreferredLanguage,
            DefaultCurrency: user.DefaultCurrency,
            DateFormat: user.DateFormat
        );
    }

    private static void SetUserProperties(UpdateUserSettingsCommand request, User user)
    {
        // Update fields if provided
        if (!string.IsNullOrWhiteSpace(request.FirstName))
            user.FirstName = request.FirstName;

        if (!string.IsNullOrWhiteSpace(request.LastName))
            user.LastName = request.LastName;

        if (!string.IsNullOrWhiteSpace(request.PreferredLanguage))
            user.PreferredLanguage = request.PreferredLanguage;

        if (!string.IsNullOrWhiteSpace(request.DefaultCurrency))
            user.DefaultCurrency = request.DefaultCurrency;

        if (!string.IsNullOrWhiteSpace(request.DateFormat))
            user.DateFormat = request.DateFormat;
    }
}

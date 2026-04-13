using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Features.UserSettings.Services;

namespace BudgetBuddy.Application.Features.UserSettings.GetUserSettings;

public class GetUserSettingsHandler(
    IUserSettingsService userSettingsService,
    ICurrentUserService currentUserService)
    : UserAwareHandler<GetUserSettingsQuery, GetUserSettingsResponse>(currentUserService)
{
    public override async Task<GetUserSettingsResponse> Handle(
        GetUserSettingsQuery request,
        CancellationToken cancellationToken)
    {
        var data = await userSettingsService.GetAsync(UserId, cancellationToken);

        return new GetUserSettingsResponse(
            UserId: data.UserId,
            Email: data.Email,
            FirstName: data.FirstName,
            LastName: data.LastName,
            PreferredLanguage: data.PreferredLanguage,
            DefaultCurrency: data.DefaultCurrency,
            DateFormat: data.DateFormat);
    }
}

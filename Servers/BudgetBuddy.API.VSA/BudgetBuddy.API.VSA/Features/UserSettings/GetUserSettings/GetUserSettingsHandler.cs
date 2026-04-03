using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Shared.Handlers;
using Microsoft.AspNetCore.Identity;

namespace BudgetBuddy.API.VSA.Features.UserSettings.GetUserSettings;

public class GetUserSettingsHandler(
    UserManager<User> userManager,
    ICurrentUserService currentUserService) : UserAwareHandler<GetUserSettingsQuery, GetUserSettingsResponse>(currentUserService)
{
    public override async Task<GetUserSettingsResponse> Handle(
        GetUserSettingsQuery request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(UserId)
            ?? throw new NotFoundException(nameof(User), UserId);

        return new GetUserSettingsResponse(
            UserId: user.Id,
            Email: user.Email ?? "",
            FirstName: user.FirstName,
            LastName: user.LastName,
            PreferredLanguage: user.PreferredLanguage,
            DefaultCurrency: user.DefaultCurrency,
            DateFormat: user.DateFormat
        );
    }
}

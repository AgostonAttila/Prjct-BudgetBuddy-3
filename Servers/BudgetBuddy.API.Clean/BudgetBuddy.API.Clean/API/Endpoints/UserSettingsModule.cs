using BudgetBuddy.Application.Features.UserSettings.GetUserSettings;
using BudgetBuddy.Application.Features.UserSettings.UpdateUserSettings;
using Microsoft.AspNetCore.Mvc;

namespace BudgetBuddy.API.Endpoints;

public class UserSettingsModule : CarterModule
{
    public UserSettingsModule() : base("/api/user-settings")
    {
        WithTags("User Settings");
        RequireAuthorization();
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("", GetUserSettings)
            .WithSummary("Get user settings")
            .WithDescription("Retrieves the current user's preferences and settings including default currency, date format, and other personalization options.")
            .WithStandardRateLimit()
            .WithName("GetUserSettings")
            .Produces<GetUserSettingsResponse>(200);

        app.MapPut("", UpdateUserSettings)
            .WithSummary("Update user settings")
            .WithDescription("Updates the authenticated user's preferences and settings including default currency, date format, and other personalization options.")
            .WithStandardRateLimit()
            .WithName("UpdateUserSettings")
            .Produces<UpdateUserSettingsResponse>(200)
            .Produces(400);
    }

    private static async Task<IResult> GetUserSettings(ISender sender)
    {
        var result = await sender.Send(new GetUserSettingsQuery());
        return Results.Ok(result);
    }

    private static async Task<IResult> UpdateUserSettings([FromBody] UpdateUserSettingsCommand command, ISender sender)
    {
        var result = await sender.Send(command);
        return Results.Ok(result);
    }
}

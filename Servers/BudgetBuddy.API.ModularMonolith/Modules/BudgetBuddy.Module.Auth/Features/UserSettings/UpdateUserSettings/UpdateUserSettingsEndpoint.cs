using Microsoft.AspNetCore.Mvc;

namespace BudgetBuddy.Module.Auth.Features.UserSettings.UpdateUserSettings;

public class UpdateUserSettingsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/user-settings", async (
            [FromBody] UpdateUserSettingsCommand command,
            ISender sender) =>
        {
            var result = await sender.Send(command);

            return Results.Ok(result);
        })
        .WithSummary("Update user settings")
        .WithDescription("Updates the authenticated user's preferences and settings including default currency, date format, and other personalization options.")
        .WithStandardRateLimit()
        .RequireAuthorization()
        .WithTags("User Settings")
        .WithName("UpdateUserSettings")
        .Produces<UpdateUserSettingsResponse>(200)
        .Produces(400)
        ;
    }
}

namespace BudgetBuddy.Module.Auth.Features.UserSettings.GetUserSettings;

public class GetUserSettingsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/user-settings", async (ISender sender) =>
        {
            var query = new GetUserSettingsQuery();
            var result = await sender.Send(query);

            return Results.Ok(result);
        })
        .WithSummary("Get user settings")
        .WithDescription("Retrieves the current user's preferences and settings including default currency, date format, and other personalization options.")
        .WithStandardRateLimit()
        .RequireAuthorization()
        .WithTags("User Settings")
        .WithName("GetUserSettings")
        .Produces<GetUserSettingsResponse>(200);
    }
}

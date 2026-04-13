using BudgetBuddy.Application.Features.TwoFactor.DisableTwoFactor;
using BudgetBuddy.Application.Features.TwoFactor.EnableTwoFactor;
using BudgetBuddy.Application.Features.TwoFactor.GetRecoveryCodes;
using BudgetBuddy.Application.Features.TwoFactor.GetTwoFactorStatus;
using BudgetBuddy.Application.Features.TwoFactor.VerifyTwoFactorSetup;
using BudgetBuddy.API.Filters;
using Microsoft.AspNetCore.Mvc;

namespace BudgetBuddy.API.Endpoints;

public record DisableTwoFactorRequest(string Password);
public record VerifyTwoFactorSetupRequest(string Code);

public class TwoFactorModule : CarterModule
{
    public TwoFactorModule() : base("/api/twofactor")
    {
        WithTags("TwoFactor");
        RequireAuthorization();
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("status", GetTwoFactorStatus)
            .WithSummary("Get two-factor authentication status")
            .WithDescription("Returns whether 2FA is enabled for the current user")
            .WithStandardRateLimit()
            .WithName("GetTwoFactorStatus");

        app.MapPost("enable", EnableTwoFactor)
            .WithSummary("Enable two-factor authentication")
            .WithDescription("Generates QR code and shared key for setting up TOTP authenticator app")
            .WithStandardRateLimit()
            .WithName("EnableTwoFactor");

        app.MapPost("disable", DisableTwoFactor)
            .WithSummary("Disable two-factor authentication")
            .WithDescription("Disables 2FA for the current user (requires password confirmation)")
            .WithStandardRateLimit()
            .WithName("DisableTwoFactor");

        app.MapPost("verify", VerifyTwoFactorSetup)
            .WithSummary("Verify two-factor authentication setup")
            .WithDescription("Verifies the TOTP code from authenticator app and enables 2FA. Protected against brute force attacks (5 attempts per 15 minutes).")
            .WithTwoFactorRateLimit()
            .WithName("VerifyTwoFactorSetup");

        app.MapPost("recovery-codes", GetRecoveryCodes)
            .WithSummary("Generate or retrieve recovery codes in batches")
            .WithDescription("Generates or retrieves backup recovery codes for account recovery. Codes are hashed before storage and each can only be used once.")
            .WithAuthRateLimit()
            .WithName("GetRecoveryCodes");
    }

    private static async Task<IResult> GetTwoFactorStatus(HttpContext context, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTwoFactorStatusQuery(context.User), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> EnableTwoFactor(HttpContext context, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new EnableTwoFactorCommand(context.User), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> DisableTwoFactor(DisableTwoFactorRequest request, HttpContext context, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DisableTwoFactorCommand(context.User, request.Password), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> VerifyTwoFactorSetup(VerifyTwoFactorSetupRequest request, HttpContext context, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new VerifyTwoFactorSetupCommand(context.User, request.Code), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetRecoveryCodes(
        HttpContext context, IMediator mediator,
        [FromQuery] int batchNumber = 1, [FromQuery] bool generateNew = false, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetRecoveryCodesQuery(context.User, batchNumber, generateNew), cancellationToken);
        return Results.Ok(result);
    }
}

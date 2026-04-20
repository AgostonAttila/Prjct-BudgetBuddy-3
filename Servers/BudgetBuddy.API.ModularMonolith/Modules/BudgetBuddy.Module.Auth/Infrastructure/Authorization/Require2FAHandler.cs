using Microsoft.AspNetCore.Authorization;

namespace BudgetBuddy.Module.Auth.Infrastructure.Authorization;

/// <summary>
/// Authorization handler that enforces 2FA requirement
/// </summary>
public class Require2FAHandler(ILogger<Require2FAHandler> logger) : AuthorizationHandler<Require2FARequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        Require2FARequirement requirement)
    {
        var amrClaim = context.User.FindFirst("amr");

        if (amrClaim != null && amrClaim.Value == "mfa")
        {
            logger.LogDebug("User has 2FA enabled, requirement satisfied");
            context.Succeed(requirement);
        }
        else
        {
            logger.LogWarning("User {UserId} attempted to access 2FA-protected resource without 2FA enabled",
                context.User.FindFirst("sub")?.Value ?? "unknown");
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Requirement that enforces 2FA authentication
/// </summary>
public class Require2FARequirement : IAuthorizationRequirement
{
}

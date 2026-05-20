using System.Security.Claims;
using BudgetBuddy.Shared.Kernel.Constants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace BudgetBuddy.Shared.Infrastructure.Extensions;

public static class KeycloakAuthExtensions
{
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services,
        IConfiguration config)
    {
        var authority = config["Keycloak:Authority"]
            ?? throw new InvalidOperationException("Keycloak:Authority is required");
        var audience = config["Keycloak:Audience"] ?? "budgetbuddy-api";
        var metadataAddress = config["Keycloak:MetadataAddress"];

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opt =>
            {
                opt.Authority            = authority;
                opt.Audience             = audience;
                opt.RequireHttpsMetadata = config.GetValue<bool>("Keycloak:RequireHttps");
                if (!string.IsNullOrEmpty(metadataAddress))
                {
                    opt.MetadataAddress = metadataAddress;
                }
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    // S-03: explicit lifetime validation (default=true, but make intent clear)
                    ValidateLifetime         = true,
                    // S-03: verify signing key is trusted (set automatically by Authority OIDC discovery,
                    // but explicit here so it is not accidentally disabled)
                    ValidateIssuerSigningKey = true,
                    // S-03: reduce default 5-min clock skew to limit token replay window
                    ClockSkew                = TimeSpan.FromSeconds(30),
                    NameClaimType            = "preferred_username",
                    RoleClaimType            = "realm_access.roles",
                };
                opt.MapInboundClaims = false;

                // S-03: reject tokens that carry no subject identity
                opt.Events = new JwtBearerEvents
                {
                    OnTokenValidated = ctx =>
                    {
                        var sub = ctx.Principal?.FindFirstValue("sub");
                        if (string.IsNullOrWhiteSpace(sub))
                        {
                            ctx.Fail("Token must contain a 'sub' claim.");
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddTransient<IClaimsTransformation, KeycloakRolesClaimsTransformation>();

        services.AddAuthorizationBuilder()
            .AddPolicy(AppPolicies.AdminOnly,      p => p.RequireRole("budgetbuddy-admin"))
            .AddPolicy(AppPolicies.AdminOrPremium, p => p.RequireRole("budgetbuddy-admin", "budgetbuddy-premium"))
            .AddPolicy(AppPolicies.Authenticated,  p => p.RequireAuthenticatedUser())
            // Require2FA: token must contain amr claim with value "mfa" or "otp"
            // Keycloak sets this when the user authenticates with a second factor
            .AddPolicy(AppPolicies.Require2FA, p => p
                .RequireAuthenticatedUser()
                .RequireAssertion(ctx =>
                    ctx.User.FindAll("amr").Any(c => c.Value is "mfa" or "otp")));

        return services;
    }
}

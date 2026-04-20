using BudgetBuddy.Module.Auth.Infrastructure.Authorization;
using BudgetBuddy.Module.Auth.Infrastructure.Middlewares;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace BudgetBuddy.Module.Auth;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // NOTE: JWT secret key validation is NOT done here.
        // It must be called after WebApplication.Build() so that WebApplicationFactory
        // test overrides (via ConfigureAppConfiguration) are already in the configuration.
        // See Program.cs — ValidateStartupConfiguration() is called post-Build().

        // Dual Authentication: Cookie (web SPA) + Bearer Token (mobile/API)
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = "DualAuth";
            options.DefaultChallengeScheme = "DualAuth";
        })
        .AddPolicyScheme("DualAuth", "Cookie or Bearer", options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                var authHeader = context.Request.Headers.Authorization.ToString();
                if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    return IdentityConstants.BearerScheme;

                return IdentityConstants.ApplicationScheme;
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AppPolicies.AdminOnly, policy =>
            {
                policy.RequireRole(AppRoles.Admin);
                policy.Requirements.Add(new Require2FARequirement());
            });

            options.AddPolicy(AppPolicies.AdminOrPremium, policy =>
                policy.RequireRole(AppRoles.Admin, AppRoles.Premium));

            options.AddPolicy(AppPolicies.Authenticated, policy =>
                policy.RequireAuthenticatedUser());

            options.AddPolicy(AppPolicies.Require2FA, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new Require2FARequirement());
            });
        });

        services.AddSingleton<IAuthorizationHandler, Require2FAHandler>();

        // Cookie hardening
        services.ConfigureApplicationCookie(opt =>
        {
            opt.Cookie.HttpOnly = true;
            opt.Cookie.SameSite = SameSiteMode.Lax;
            opt.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            opt.Events.OnRedirectToLogin = ctx =>
            {
                if (ctx.Request.Path.StartsWithSegments("/api"))
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                else
                    ctx.Response.Redirect(ctx.RedirectUri);
                return Task.CompletedTask;
            };
            opt.Events.OnRedirectToAccessDenied = ctx =>
            {
                if (ctx.Request.Path.StartsWithSegments("/api"))
                    ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                else
                    ctx.Response.Redirect(ctx.RedirectUri);
                return Task.CompletedTask;
            };
        });

        // Antiforgery – CSRF
        services.AddAntiforgery(opt =>
        {
            opt.HeaderName = "X-XSRF-TOKEN";
        });

        return services;
    }

    public static WebApplication UseAuthenticationServices(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();

        // ⚠️ SECURITY TODO: Block insecure /auth/refresh endpoint
        // app.UseDisableInsecureRefreshEndpoint();

        // Token blacklist check — must be after authentication
        app.UseTokenBlacklist();

        // Security event logging — must be after authentication to capture user claims
        app.UseMiddleware<SecurityEventMiddleware>();

        // CSRF validation for /api and /auth endpoints
        app.Use(async (ctx, next) =>
        {
            if (HttpMethods.IsGet(ctx.Request.Method) ||
                HttpMethods.IsHead(ctx.Request.Method) ||
                HttpMethods.IsOptions(ctx.Request.Method))
            {
                await next();
                return;
            }

            if (ctx.Request.Path.Equals("/auth/csrf-token", StringComparison.OrdinalIgnoreCase))
            {
                await next();
                return;
            }

            var publicAuthEndpoints = new[]
            {
                "/auth/register",
                "/auth/login",
                "/auth/refresh",
                "/auth/token/refresh"
            };

            if (publicAuthEndpoints.Any(endpoint =>
                ctx.Request.Path.StartsWithSegments(endpoint, StringComparison.OrdinalIgnoreCase)))
            {
                await next();
                return;
            }

            var requiresCsrfValidation =
                ctx.Request.Path.StartsWithSegments("/api") ||
                ctx.Request.Path.StartsWithSegments("/auth");

            if (requiresCsrfValidation)
            {
                var authHeader = ctx.Request.Headers.Authorization.ToString();
                if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    var antiforgery = ctx.RequestServices.GetRequiredService<IAntiforgery>();
                    await antiforgery.ValidateRequestAsync(ctx);
                }
            }

            await next();
        });

        return app;
    }
}

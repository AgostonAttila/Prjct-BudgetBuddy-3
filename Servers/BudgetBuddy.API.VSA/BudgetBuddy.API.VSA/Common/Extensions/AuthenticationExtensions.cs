using BudgetBuddy.API.VSA.Common.Authorization;
using BudgetBuddy.API.VSA.Common.Infrastructure.Security.Authentication;
using BudgetBuddy.API.VSA.Common.Middlewares;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace BudgetBuddy.API.VSA.Common.Extensions;

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
            // Use DualAuth as default to support both Cookie and Bearer
            options.DefaultScheme = "DualAuth";
            options.DefaultChallengeScheme = "DualAuth";
        })
        .AddPolicyScheme("DualAuth", "Cookie or Bearer", options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                // If Authorization header exists with Bearer, use Bearer scheme
                var authHeader = context.Request.Headers.Authorization.ToString();
                if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    return IdentityConstants.BearerScheme;
                }
                // Otherwise use Cookie scheme
                return IdentityConstants.ApplicationScheme;
            };
        });

        services.AddAuthorization(options =>
        {
            // Admin-only policy (with 2FA enforcement)
            options.AddPolicy(AppPolicies.AdminOnly, policy =>
            {
                policy.RequireRole(AppRoles.Admin);
                policy.Requirements.Add(new Require2FARequirement()); // Enforce 2FA for admin
            });

            // Admin or Premium policy
            options.AddPolicy(AppPolicies.AdminOrPremium, policy =>
                policy.RequireRole(AppRoles.Admin, AppRoles.Premium));

            // Authenticated user policy
            options.AddPolicy(AppPolicies.Authenticated, policy =>
                policy.RequireAuthenticatedUser());

            // Require 2FA policy
            options.AddPolicy(AppPolicies.Require2FA, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new Require2FARequirement());
            });
        });

        // Register authorization handlers
        services.AddSingleton<IAuthorizationHandler, Require2FAHandler>();

        // Identity API Endpoints - cookie + Identity bearer token
        services.AddIdentityApiEndpoints<User>(options =>
        {
            // Lockout
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
            // Password
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 10;
            options.Password.RequiredUniqueChars = 2;
            // SignIn - email confirmation NOT required in Development or Testing (no email service)
            var currentEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? "Production";
            var isDevelopmentOrTesting = currentEnv.Equals("Development", StringComparison.OrdinalIgnoreCase)
                || currentEnv.Equals("Testing", StringComparison.OrdinalIgnoreCase);
            options.SignIn.RequireConfirmedEmail = !isDevelopmentOrTesting;
            options.SignIn.RequireConfirmedPhoneNumber = false;
            // User
            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            options.User.RequireUniqueEmail = true;
        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddTokenProvider<AuthenticatorTokenProvider<User>>("Authenticator");

        // Configure Identity Bearer Token lifetime.
        // NOTE: This is NOT a JWT — AddIdentityApiEndpoints uses ASP.NET Core Identity's
        // built-in opaque bearer token (Data Protection encrypted, stateless).
        // BearerTokenExpiration  → access token lifetime  (default: 1 hour)
        // RefreshTokenExpiration → refresh token lifetime (default: 14 days)
        services.Configure<BearerTokenOptions>(IdentityConstants.BearerScheme, options =>
        {
            options.BearerTokenExpiration  = TimeSpan.FromMinutes(15); // access token: short-lived for security
            options.RefreshTokenExpiration = TimeSpan.FromDays(7);     // refresh token: user convenience
        });


        //services.AddFido2(opt => { });

        // Cookie hardening
        services.ConfigureApplicationCookie(opt =>
        {
            opt.Cookie.HttpOnly = true;
            opt.Cookie.SameSite = SameSiteMode.Lax;   // Strict block a cross-origin XHR-t SPA
            opt.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            // For /api/* paths return 401/403 instead of redirecting to a login page.
            // SPA routes (non /api/*) still get the normal login redirect.
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
            opt.HeaderName = "X-XSRF-TOKEN"; // UI send this header
        });

        return services;
    }

    public static WebApplication UseAuthenticationServices(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();

        // ⚠️ SECURITY TODO: Block insecure /auth/refresh endpoint
        // Requires full custom auth implementation (login, register, logout)
        // See: Common/Middlewares/DisableInsecureRefreshEndpointMiddleware.cs
        // app.UseDisableInsecureRefreshEndpoint();

        // Token blacklist check - must be after authentication
        // Dev: uses in-memory distributed cache (state lost on restart, acceptable for dev)
        // Prod: uses Redis (persistent)
        app.UseTokenBlacklist();

        // Security event logging - must be after authentication to capture user claims
        app.UseMiddleware<SecurityEventMiddleware>();

        // CSRF validation for API and Auth endpoints
        app.Use(async (ctx, next) =>
        {
            // Skip GET, HEAD, OPTIONS (safe methods)
            if (HttpMethods.IsGet(ctx.Request.Method) ||
                HttpMethods.IsHead(ctx.Request.Method) ||
                HttpMethods.IsOptions(ctx.Request.Method))
            {
                await next();
                return;
            }

            // Skip CSRF token generation endpoint itself
            if (ctx.Request.Path.Equals("/auth/csrf-token", StringComparison.OrdinalIgnoreCase))
            {
                await next();
                return;
            }

            // Skip public auth endpoints (no session/auth yet, CSRF not applicable)
            var publicAuthEndpoints = new[]
            {
                "/auth/register",
                "/auth/login",
                "/auth/refresh",         // ⚠️ Built-in (NO reuse detection - security risk!)
                "/auth/token/refresh"    // ✅ Custom secure endpoint (WITH reuse detection)
                // TODO: Replace /auth/refresh with custom implementation
            };

            if (publicAuthEndpoints.Any(endpoint =>
                ctx.Request.Path.StartsWithSegments(endpoint, StringComparison.OrdinalIgnoreCase)))
            {
                await next();
                return;
            }

            // Validate CSRF for /api and /auth endpoints (except specific exclusions above)
            var requiresCsrfValidation =
                ctx.Request.Path.StartsWithSegments("/api") ||
                ctx.Request.Path.StartsWithSegments("/auth");

            if (requiresCsrfValidation)
            {
                // Skip CSRF validation if using Bearer token (JWT) - CSRF is only for cookie-based auth
                var authHeader = ctx.Request.Headers.Authorization.ToString();
                if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    // Cookie-based auth -> validate CSRF
                    var antiforgery = ctx.RequestServices.GetRequiredService<IAntiforgery>();
                    await antiforgery.ValidateRequestAsync(ctx);
                }
            }

            await next();
        });

        return app;
    }

    public static void MapAuthenticationEndpoints(this WebApplication app)
    {
        var authGroup = app.MapGroup("/auth");

        // Identity API endpoints
        //
        // Cookie auth (web SPA):
        //   POST /auth/login?useCookies=true
        //   POST /auth/logout
        //
        // Bearer token (mobile/API):
        //   POST /auth/login (default, returns accessToken + refreshToken)
        //   POST /auth/refresh ⚠️ ACTIVE but INSECURE (no reuse detection!)
        //
        // Common:
        //   POST /auth/register
        //
        // ⚠️ SECURITY NOTE:
        // The built-in /auth/refresh endpoint lacks token reuse detection (OWASP vulnerability).
        // A custom /auth/token/refresh endpoint exists with proper security, but it's INCOMPATIBLE
        // with the built-in /auth/login (different token stores: memory vs database).
        //
        // TODO: Implement full custom auth (login, register, logout) to use secure refresh.
        // See: Features/Auth/RefreshToken/ and Common/Middlewares/DisableInsecureRefreshEndpointMiddleware.cs
        authGroup.MapIdentityApi<User>()
            .WithAuthRateLimit()
            .WithTags("Authentication");

        // CSRF token endpoint – Angular (cookie auth) számára
        // Note: NO authentication required - token generation is safe, validation is what matters
        authGroup.MapGet("/csrf-token", (IAntiforgery antiforgery, HttpContext ctx) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(ctx);
            ctx.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!, new CookieOptions
            {
                HttpOnly = false,  // UI reads from JS
                Secure = true,
                SameSite = SameSiteMode.Lax,
            });
            return Results.NoContent();
        })
        .RequireRateLimiting("fixedByIp")  // IP-based rate limiting for anonymous users
        .WithSummary("Get CSRF token")
        .WithDescription("Retrieves a CSRF token for subsequent authenticated requests. Rate limited by IP to prevent abuse.")
        .WithTags("Authentication")
        .WithName("GetCsrfToken");
    }
}
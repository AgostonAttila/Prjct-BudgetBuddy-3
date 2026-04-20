using BudgetBuddy.Module.Auth.Infrastructure;
using BudgetBuddy.Module.Auth.Infrastructure.Authentication;
using BudgetBuddy.Module.Auth.Infrastructure.Filters;
using BudgetBuddy.Module.Auth.Persistence;
using BudgetBuddy.Module.Auth.Persistence.Seeders;
using BudgetBuddy.Module.Auth.Services;
using BudgetBuddy.Shared.Contracts;
using BudgetBuddy.Shared.Infrastructure.Extensions;
using BudgetBuddy.Module.Auth.Infrastructure.Authentication;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetBuddy.Module.Auth;

public class AuthModule : IModule
{
    public string ModuleName => "Auth";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // DbContext (Identity tables + RefreshTokens + SecurityEvents in "auth" schema)
        services.AddModuleDbContext<AuthDbContext>(configuration);

        // Identity — EF store uses AuthDbContext
        services.AddIdentityApiEndpoints<User>(options =>
        {
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 10;
            options.Password.RequiredUniqueChars = 2;
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                      ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                      ?? "Production";
            options.SignIn.RequireConfirmedEmail = !env.Equals("Development", StringComparison.OrdinalIgnoreCase)
                                                  && !env.Equals("Testing", StringComparison.OrdinalIgnoreCase);
            options.SignIn.RequireConfirmedPhoneNumber = false;
            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            options.User.RequireUniqueEmail = true;
        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<AuthDbContext>()
        .AddTokenProvider<AuthenticatorTokenProvider<User>>("Authenticator");

        // Bearer token lifetime configuration
        services.Configure<BearerTokenOptions>(IdentityConstants.BearerScheme, options =>
        {
            options.BearerTokenExpiration = TimeSpan.FromMinutes(15);
            options.RefreshTokenExpiration = TimeSpan.FromDays(7);
        });

        // Auth-specific services
        services.AddScoped<ITokenService, TokenService>();
        services.AddSingleton<ITokenBlacklistService, TokenBlacklistService>();
        services.AddScoped<ISecurityEventService, SecurityEventService>();
        services.AddScoped<TwoFactorRateLimitFilter>();
        // Registered as ISeeder so SeedDatabaseAsync() can resolve them in order
        services.AddScoped<ISeeder, RoleSeeder>();
        services.AddScoped<ISeeder, AdminUserSeeder>();

        services.AddScoped<IAuthenticationEmailService, AuthenticationEmailService>();
        services.AddScoped<IUserCurrencyService, UserCurrencyService>();

        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var app = (WebApplication)endpoints;
        var authGroup = app.MapGroup("/auth");

        // Identity API endpoints (Cookie + Bearer)
        // ⚠️ SECURITY NOTE: built-in /auth/refresh lacks token reuse detection.
        // Custom /auth/token/refresh exists but is incompatible with built-in login.
        // TODO: Implement full custom auth to use secure refresh exclusively.
        authGroup.MapIdentityApi<User>()
            .WithAuthRateLimit()
            .WithTags("Authentication");

        // CSRF token endpoint – for Angular (cookie auth)
        authGroup.MapGet("/csrf-token", (IAntiforgery antiforgery, HttpContext ctx) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(ctx);
            ctx.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!, new CookieOptions
            {
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.Lax,
            });
            return Results.NoContent();
        })
        .RequireRateLimiting("fixedByIp")
        .WithSummary("Get CSRF token")
        .WithDescription("Retrieves a CSRF token for subsequent authenticated requests. Rate limited by IP to prevent abuse.")
        .WithTags("Authentication")
        .WithName("GetCsrfToken");

        return endpoints;
    }
}

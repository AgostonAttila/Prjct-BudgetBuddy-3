using BudgetBuddy.API.Infrastructure;
using BudgetBuddy.Infrastructure.Extensions;

namespace BudgetBuddy.API.Extensions;

public static class CachingExtensions
{
    public static void AddCaching(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // Distributed cache, memory cache, hybrid cache (Infrastructure concern)
        services.AddInfrastructureCaching(configuration, environment);

        // ICacheTagInvalidator wraps IOutputCacheStore (ASP.NET Core type, only available in web project)
        services.AddScoped<ICacheTagInvalidator, OutputCacheTagInvalidator>();

        services.AddOutputCache(options =>
        {
            // Base policy: 1 minute expiration for unconfigured endpoints
            options.AddBasePolicy(builder => builder.Expire(TimeSpan.FromMinutes(1)));

            // ============================================================
            // HIGH PRIORITY - Complex queries, frequently accessed
            // ============================================================

            // Dashboard endpoint - aggregates multiple data sources
            // 1 min: user expects fresh data after adding a transaction
            options.AddPolicy("dashboard", builder =>
                builder.Expire(TimeSpan.FromMinutes(1))
                    .Tag("dashboard")
                    .SetVaryByHeader("Authorization")
                    .VaryByValue(ctx =>
                    {
                        var userId = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
                        return new KeyValuePair<string, string>("userId", userId);
                    }));

            // Monthly summary report
            options.AddPolicy("monthly-summary", builder =>
                builder.Expire(TimeSpan.FromMinutes(5))
                    .Tag("monthly-summary")
                    .SetVaryByHeader("Authorization")
                    .SetVaryByQuery("year", "month", "accountId")
                    .VaryByValue(ctx =>
                    {
                        var userId = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
                        return new KeyValuePair<string, string>("userId", userId);
                    }));

            // Income vs expense report
            options.AddPolicy("income-vs-expense", builder =>
                builder.Expire(TimeSpan.FromMinutes(5))
                    .Tag("income-vs-expense")
                    .SetVaryByHeader("Authorization")
                    .SetVaryByQuery("startDate", "endDate", "accountId")
                    .VaryByValue(ctx =>
                    {
                        var userId = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
                        return new KeyValuePair<string, string>("userId", userId);
                    }));

            // Spending by category report
            options.AddPolicy("spending-by-category", builder =>
                builder.Expire(TimeSpan.FromMinutes(5))
                    .Tag("spending-by-category")
                    .SetVaryByHeader("Authorization")
                    .SetVaryByQuery("startDate", "endDate", "accountId")
                    .VaryByValue(ctx =>
                    {
                        var userId = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
                        return new KeyValuePair<string, string>("userId", userId);
                    }));

            // Investment performance report
            options.AddPolicy("investment-performance", builder =>
                builder.Expire(TimeSpan.FromMinutes(5))
                    .Tag("investment-performance")
                    .SetVaryByHeader("Authorization")
                    .SetVaryByQuery("startDate", "endDate", "type")
                    .VaryByValue(ctx =>
                    {
                        var userId = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
                        return new KeyValuePair<string, string>("userId", userId);
                    }));

            // Budget vs actual comparison
            options.AddPolicy("budget-vs-actual", builder =>
                builder.Expire(TimeSpan.FromMinutes(5))
                    .Tag("budget-vs-actual")
                    .SetVaryByHeader("Authorization")
                    .SetVaryByQuery("year", "month")
                    .VaryByValue(ctx =>
                    {
                        var userId = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
                        return new KeyValuePair<string, string>("userId", userId);
                    }));

            // ============================================================
            // MEDIUM PRIORITY - Master data, less frequent changes
            // ============================================================

            // Categories list - rarely changes
            options.AddPolicy("categories", builder =>
                builder.Expire(TimeSpan.FromMinutes(30))
                    .Tag("categories")
                    .SetVaryByHeader("Authorization")
                    .VaryByValue((context) =>
                    {                       
                        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
                        return new KeyValuePair<string, string>("userId", userId);
                    }));

            // Currencies list - very stable data
            options.AddPolicy("currencies", builder =>
                builder.Expire(TimeSpan.FromMinutes(60))
                    .Tag("currencies")
                    .SetVaryByHeader("Authorization")
                    .VaryByValue((context) =>
                    {                        
                        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
                        return new KeyValuePair<string, string>("userId", userId);
                    }));

            // Category types with pagination
            options.AddPolicy("category-types", builder =>
                builder.Expire(TimeSpan.FromMinutes(30))
                    .Tag("category-types")
                    .SetVaryByHeader("Authorization")
                    .SetVaryByQuery("categoryId", "page", "pageSize")
                    .VaryByValue(ctx =>
                    {
                        var userId = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
                        return new KeyValuePair<string, string>("userId", userId);
                    }));

            // Budgets list with filters
            options.AddPolicy("budgets-list", builder =>
                builder.Expire(TimeSpan.FromMinutes(15))
                    .Tag("budgets-list")
                    .SetVaryByHeader("Authorization")
                    .SetVaryByQuery("year", "month", "categoryId")
                    .VaryByValue(ctx =>
                    {
                        var userId = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
                        return new KeyValuePair<string, string>("userId", userId);
                    }));

            // Budget alerts
            options.AddPolicy("budget-alerts", builder =>
                builder.Expire(TimeSpan.FromMinutes(10))
                    .Tag("budget-alerts")
                    .SetVaryByHeader("Authorization")
                    .SetVaryByQuery("year", "month")
                    .VaryByValue(ctx =>
                    {
                        var userId = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
                        return new KeyValuePair<string, string>("userId", userId);
                    }));

            // Accounts list
            options.AddPolicy("accounts-list", builder =>
                builder.Expire(TimeSpan.FromMinutes(5))
                    .Tag("accounts-list")
                    .SetVaryByHeader("Authorization")
                    .VaryByValue((context) =>
                    {                      
                        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
                        return new KeyValuePair<string, string>("userId", userId);
                    }));

            // Account balance - specific account
            options.AddPolicy("account-balance", builder =>
                builder.Expire(TimeSpan.FromMinutes(5))
                    .Tag("account-balance")
                    .SetVaryByHeader("Authorization")
                    .SetVaryByRouteValue("id")
                    .VaryByValue(ctx =>
                    {
                        var userId = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
                        return new KeyValuePair<string, string>("userId", userId);
                    }));

            // Portfolio value calculation
            options.AddPolicy("portfolio-value", builder =>
                builder.Expire(TimeSpan.FromMinutes(5))
                    .Tag("portfolio-value")
                    .SetVaryByHeader("Authorization")
                    .SetVaryByQuery("currency")
                    .VaryByValue(ctx =>
                    {
                        var userId = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
                        return new KeyValuePair<string, string>("userId", userId);
                    }));

            // ============================================================
            // LOW PRIORITY - Transactional data with many filters
            // ============================================================

            // Transactions list - 1 min: frequent writes, stale data frustrates users
            options.AddPolicy("transactions", builder =>
                builder.Expire(TimeSpan.FromMinutes(1))
                    .Tag("transactions")
                    .SetVaryByHeader("Authorization")
                    .SetVaryByQuery(
                        "startDate", "endDate", "accountId", "categoryId",
                        "type", "search", "page", "pageSize")
                    .VaryByValue(ctx =>
                    {
                        var userId = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
                        return new KeyValuePair<string, string>("userId", userId);
                    }));

            // Investments list
            options.AddPolicy("investments", builder =>
                builder.Expire(TimeSpan.FromMinutes(2))
                    .Tag("investments")
                    .SetVaryByHeader("Authorization")
                    .SetVaryByQuery("type", "search")
                    .VaryByValue(ctx =>
                    {
                        var userId = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
                        return new KeyValuePair<string, string>("userId", userId);
                    }));
        });
    }
}
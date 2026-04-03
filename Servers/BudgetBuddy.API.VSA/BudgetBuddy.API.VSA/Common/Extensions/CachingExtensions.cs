using Microsoft.Extensions.Caching.Hybrid;

namespace BudgetBuddy.API.VSA.Common.Extensions;

public static class CachingExtensions
{
    public static void AddCaching(this IServiceCollection services,IConfiguration configuration, IHostEnvironment environment)
    {
        
        // Idempotency & Token Blacklist - Distributed Cache for storing idempotency keys and blacklisted tokens
        // Development: In-memory cache (non-persistent, single instance only)
        // Production: Redis (persistent, supports multiple instances, required for horizontal scaling)
        if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
        {
            services.AddDistributedMemoryCache();
        }
        else
        {
            var redisConnection = configuration.GetConnectionString("Redis");
            if (string.IsNullOrEmpty(redisConnection))
            {
                throw new InvalidOperationException(
                    "Redis connection string is required for production environment. " +
                    "Set 'ConnectionStrings:Redis' in appsettings.Production.json or environment variable 'ConnectionStrings__Redis'.");
            }

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "BudgetBuddy:";
            });
        }
        
        
        
        // Memory cache configuration
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 20000; // Max 20,000 cached items
            options.CompactionPercentage = 0.05; // Compact 5% when limit reached
        });
        
        // Hybrid Cache (L1 in-memory + L2 distributed) for Dashboard, Reports, Reference Data
        // Uses IDistributedCache as L2 backend (memory in dev, Redis in production)
        // L1 (in-memory): Ultra-fast, instance-local, short TTL (1 min)
        // L2 (distributed): Shared across instances, longer TTL (5 min)
        services.AddHybridCache(options =>
        {
            // L1 cache size limit per instance (prevents memory issues)
            options.MaximumPayloadBytes = 5 * 1024 * 1024; // 5 MB per instance
            options.MaximumKeyLength = 512;

            // Default TTLs (can be overridden per request)
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),       // L2 (Redis/Memory): 5 min
                LocalCacheExpiration = TimeSpan.FromMinutes(1) // L1 (in-memory): 1 min
            };
        });

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
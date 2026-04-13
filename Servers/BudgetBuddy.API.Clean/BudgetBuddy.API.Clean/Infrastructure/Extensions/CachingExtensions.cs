using Microsoft.Extensions.Caching.Hybrid;

namespace BudgetBuddy.Infrastructure.Extensions;

public static class CachingExtensions
{
    public static void AddInfrastructureCaching(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
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
    }
}

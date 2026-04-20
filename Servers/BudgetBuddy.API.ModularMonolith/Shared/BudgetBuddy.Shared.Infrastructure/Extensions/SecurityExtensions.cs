using System.Threading.RateLimiting;
using Azure.Identity;
using BudgetBuddy.Shared.Infrastructure.Configuration;
using BudgetBuddy.Shared.Infrastructure.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;


namespace BudgetBuddy.Shared.Infrastructure.Extensions;

public static class SecurityExtensions
{
    public static WebApplicationBuilder AddSecurity(this WebApplicationBuilder builder, IConfiguration configuration)
    {
        const long maxRequestBodySize = 10 * 1024 * 1024; // 10 MB for file uploads

        builder.Services.Configure<IISServerOptions>(opt =>
        {
            opt.MaxRequestBodySize = maxRequestBodySize;
        });

        // Kestrel limits – DoS protection + HTTP/2 support
        builder.WebHost.ConfigureKestrel(opt =>
        {
            opt.Limits.MaxRequestBodySize = maxRequestBodySize;  // Aligned with IIS: 10 MB
            opt.Limits.MaxRequestHeaderCount = 20;
            opt.Limits.MaxRequestHeadersTotalSize = 32_768; // 32 KB
            opt.AddServerHeader = false;
       
            opt.ConfigureEndpointDefaults(listenOptions =>
            {
                listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
            });
        });

        // CORS with production validation
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
   
        // CORS origin validation only runs in Production.
        // Development and Testing environments allow localhost origins.
        if (builder.Environment.IsProduction())
        {
            var invalidOrigins = allowedOrigins.Where(origin =>
                origin.Contains("localhost", StringComparison.OrdinalIgnoreCase) ||
                origin.Contains("your-production-domain", StringComparison.OrdinalIgnoreCase) ||
                origin.Contains("example.com", StringComparison.OrdinalIgnoreCase) ||
                origin.Contains("placeholder", StringComparison.OrdinalIgnoreCase) ||
                origin == "*"
            ).ToArray();

            if (invalidOrigins.Any())
            {
                throw new InvalidOperationException(
                    $"SECURITY: Invalid CORS origins detected in production: {string.Join(", ", invalidOrigins)}. " +
                    "Update Cors:AllowedOrigins in appsettings.Production.json with actual production domains.");
            }

            if (!allowedOrigins.Any())
            {
                throw new InvalidOperationException(
                    "SECURITY: No CORS origins configured for production. " +
                    "Set Cors:AllowedOrigins in appsettings.Production.json.");
            }
        }

        builder.Services.AddCors(opt =>
        {
            opt.AddPolicy("Default", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .WithMethods("GET", "POST", "PUT", "DELETE")
                    .WithHeaders("Content-Type", "Authorization", "X-Correlation-ID", "X-XSRF-TOKEN")
                    .AllowCredentials()           // need HttpOnly cookie send from SPA
                    .SetPreflightMaxAge(TimeSpan.FromHours(1));
            });
        });

        builder.Services.AddRateLimiting();

        // Data Protection API — key storage strategy per environment:
        // Production: Azure Key Vault (keys survive container restarts and horizontal scaling)
        // Dev/Staging: Local file system (simpler, no cloud dependency)
        var keyVaultUrl = builder.Configuration["KeyVault:Url"];
        var dataProtection = builder.Services.AddDataProtection()
            .SetApplicationName("BudgetBuddy");

        if (builder.Environment.IsProduction() && !string.IsNullOrWhiteSpace(keyVaultUrl))
        {
            var keyIdentifier = new Uri($"{keyVaultUrl.TrimEnd('/')}/keys/data-protection");
            dataProtection.ProtectKeysWithAzureKeyVault(keyIdentifier, new DefaultAzureCredential());
        }
        else
        {
            dataProtection.PersistKeysToFileSystem(
                new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "keys")));
        }

        //HSTS
        if (!builder.Environment.IsDevelopment())
        {
            builder.Services.AddHsts(opt =>
            {
                opt.MaxAge = TimeSpan.FromDays(365);
                opt.IncludeSubDomains = true;
                opt.Preload = true;
            });
        }

        // Request Timeouts – global 30sec, individual endpoints .WithRequestTimeout()-tal
        var timeoutSeconds = configuration.GetValue<int>("RequestTimeoutSeconds", 35);

        builder.Services.AddRequestTimeouts(options =>
        {
            options.DefaultPolicy = new RequestTimeoutPolicy
            {
                Timeout = TimeSpan.FromSeconds(timeoutSeconds),
                WriteTimeoutResponse = async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "Request timeout",
                        message = "The request took too long to process."
                    });
                }
            };
        });
        // Graceful Shutdown - Configure timeout and tracking
        var shutdownTimeout = configuration.GetValue<int>("ShutdownTimeoutSeconds", 35);

        builder.Host.ConfigureHostOptions(options =>
        {
            options.ShutdownTimeout = TimeSpan.FromSeconds(shutdownTimeout);
        });

        // Register graceful shutdown tracking service for observability
        builder.Services.AddHostedService<GracefulShutdownService>();

        return builder;
    }

    private static void AddRateLimiting(this IServiceCollection services)
    {
        // Register rate limiter services. Policies are configured separately below
        // via AddOptions<RateLimiterOptions>().Configure<TDep>() to properly inject
        // IOptions<RateLimitConfig> through DI — avoids the services.BuildServiceProvider()
        // anti-pattern which creates an unmanaged second DI container.
        services.AddRateLimiter(options =>
        {
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    await context.HttpContext.Response.WriteAsJsonAsync(new
                    {
                        error = "Too many requests",
                        retryAfter = retryAfter.TotalSeconds
                    }, cancellationToken: token);
                }
                else
                {
                    await context.HttpContext.Response.WriteAsJsonAsync(new
                    {
                        error = "Too many requests. Please try again later."
                    }, cancellationToken: token);
                }
            };
        });

        services.AddOptions<RateLimiterOptions>()
            .Configure<IOptions<RateLimitConfig>>((options, rateLimitOptions) =>
            {
                var config = rateLimitOptions.Value;

                // Fixed Window - general protection for all endpoints
                options.AddFixedWindowLimiter("fixed", opt =>
                {
                    opt.PermitLimit = config.Fixed.PermitLimit;
                    opt.Window = TimeSpan.FromMinutes(config.Fixed.WindowMinutes);
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = config.Fixed.QueueLimit;
                });

                // Fixed Window with IP-based partitioning (separate policy)
                options.AddPolicy("fixedByIp", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = config.FixedByIp.PermitLimit,
                            Window = TimeSpan.FromMinutes(config.FixedByIp.WindowMinutes),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = config.FixedByIp.QueueLimit
                        }));

                // Token Bucket - for login endpoint: allows short bursts while limiting overall rate
                options.AddTokenBucketLimiter("auth", opt =>
                {
                    opt.TokenLimit = config.Auth.TokenLimit;
                    opt.ReplenishmentPeriod = TimeSpan.FromMinutes(config.Auth.ReplenishmentPeriodMinutes);
                    opt.TokensPerPeriod = config.Auth.TokensPerPeriod;
                    opt.AutoReplenishment = true;
                });

                // Fixed Window by IP - for refresh token endpoint (brute force protection)
                // Refresh tokens are 512-bit random, so brute force is impossible,
                // but we still want to prevent abuse (e.g., stolen token testing).
                // Access token expires every 15 min → 1 refresh per window is normal; 3 gives headroom.
                options.AddPolicy("refresh", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = config.Refresh.PermitLimit,
                            Window = TimeSpan.FromMinutes(config.Refresh.WindowMinutes),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = config.Refresh.QueueLimit
                        }));

                // Global limit — safety net above all specific limits.
                // Prevents bypass attempts while allowing legitimate traffic spikes.
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
                    RateLimitPartition.GetFixedWindowLimiter("global",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = config.Global.PermitLimit,
                            Window = TimeSpan.FromMinutes(config.Global.WindowMinutes)
                        }));

                // Sliding Window - for resource-intensive endpoints without an API key.
                // Adapts to traffic patterns better than fixed window.
                options.AddSlidingWindowLimiter("api", opt =>
                {
                    opt.PermitLimit = config.Api.PermitLimit;
                    opt.Window = TimeSpan.FromMinutes(config.Api.WindowMinutes);
                    opt.SegmentsPerWindow = config.Api.SegmentsPerWindow;
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = config.Api.QueueLimit;
                });
            });
    }


    public static HeaderPolicyCollection GetSecurityHeaders(bool isDevelopment)
    {
        return new HeaderPolicyCollection()
              .AddFrameOptionsDeny()
              .AddXssProtectionBlock()
              .AddContentTypeOptionsNoSniff()
              .AddReferrerPolicyStrictOriginWhenCrossOrigin()
              .AddCrossOriginOpenerPolicy(builder => builder.SameOrigin())
              .AddCrossOriginResourcePolicy(builder => builder.SameOrigin())
              .AddCrossOriginEmbedderPolicy(builder => builder.RequireCorp())
              .AddContentSecurityPolicy(builder =>
              {
                  builder.AddDefaultSrc().Self();
                  if (isDevelopment)
                  {
                      builder.AddScriptSrc()
                            .Self()
                            .UnsafeInline()
                            .UnsafeEval();

                      builder.AddStyleSrc()
                             .Self()
                             .UnsafeInline();
                  }
                  else
                  {
                      builder.AddScriptSrc().Self();
                      builder.AddStyleSrc().Self();
                  }
                  builder.AddImgSrc().Self();
                  builder.AddFontSrc().Self();
                  builder.AddConnectSrc().Self();
              })
              .RemoveServerHeader();
    }


    public static WebApplication UseSecurity(this WebApplication app)
    {
        if (app.Environment.IsProduction())
            app.UseHsts();

        app.UseCors("Default");
        // Skip HTTPS redirect in Testing — HttpClient strips Authorization headers on cross-scheme redirects
        if (!app.Environment.IsEnvironment("Testing"))
            app.UseHttpsRedirection();
        app.UseRequestTimeouts();
        app.UseRateLimiter();

        return app;
    }

}
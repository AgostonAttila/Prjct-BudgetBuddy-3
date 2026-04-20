using BudgetBuddy.Shared.Infrastructure.Logging;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using System.Text.Json;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Exporter;

namespace BudgetBuddy.Shared.Infrastructure.Extensions;

public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        // Serilog
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            // PII masking: Enabled in Production, Disabled in Development (for debugging)
            var piiMaskingEnabled = !context.HostingEnvironment.IsDevelopment();

            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.With(new PiiMaskingEnricher(piiMaskingEnabled))  // Mask PII in log messages
                .Destructure.With(new SensitiveDataDestructuringPolicy());  // Mask [SensitiveData] properties
        });





        // Health Checks - Database, Redis, ClamAV, External Services
        var healthChecks = builder.Services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>("database", tags: ["ready", "live"]);

        // Redis health check (production only - dev uses in-memory cache)
        if (!builder.Environment.IsDevelopment())
        {
            var redisConnection = builder.Configuration.GetConnectionString("Redis");
            if (!string.IsNullOrEmpty(redisConnection))
            {
                healthChecks.AddRedis(
                    redisConnection,
                    name: "redis",
                    tags: ["ready", "cache"]
                );
            }
        }

        // ClamAV health check (if enabled)
        var clamAvEnabled = builder.Configuration.GetValue<bool>("ClamAV:Enabled");
        if (clamAvEnabled)
        {
            var clamAvServer = builder.Configuration.GetValue<string>("ClamAV:ServerUrl") ?? "localhost";
            var clamAvPort = builder.Configuration.GetValue<int>("ClamAV:Port");

            healthChecks.AddTcpHealthCheck(
                options => options.AddHost(clamAvServer, clamAvPort),
                name: "clamav",
                tags: ["ready", "antivirus"]
            );
        }

        // OpenTelemetry – distributed tracing + metrics
        var otlpEndpoint = builder.Configuration.GetValue<string>("OpenTelemetry:OtlpEndpoint") ?? "http://localhost:4317";

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(
                serviceName: "BudgetBuddy.API",
                serviceVersion: "1.0.0"))
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.EnrichWithHttpRequest = (activity, httpRequest) =>
                    {
                        activity.SetTag("http.request.path", httpRequest.Path);
                        activity.SetTag("http.request.method", httpRequest.Method);
                    };
                })
                .AddHttpClientInstrumentation(options =>
                {
                    options.RecordException = true;
                });

                // Always export to OTLP (Jaeger)
                tracing.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                    options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                });

                // Console exporter for development debugging
                if (builder.Environment.IsDevelopment())
                    tracing.AddConsoleExporter();
            })
            .WithMetrics(metrics =>
            {
                metrics.AddMeter("Microsoft.AspNetCore.Hosting")
                       .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
                       .AddMeter("System.Net.Http")
                       .AddRuntimeInstrumentation()
                       .AddPrometheusExporter();
            });

        return builder;
    }

    public static void MapObservabilityEndpoints(this WebApplication app)
    {
        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        // Health check endpoint-ok
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (ctx, report) =>
            {
                ctx.Response.ContentType = "application/json";
                var result = new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString() })
                };
                await ctx.Response.WriteAsync(JsonSerializer.Serialize(result, jsonOptions));
            }
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = async (ctx, report) =>
            {
                ctx.Response.ContentType = "application/json";
                var result = new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        duration = e.Value.Duration.TotalMilliseconds
                    })
                };
                await ctx.Response.WriteAsync(JsonSerializer.Serialize(result, jsonOptions));
            }
        });

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false  // checks only app liveness, no DB checks
        });

        // Prometheus metrics scraping endpoint – /metrics
        app.MapPrometheusScrapingEndpoint();
    }
}
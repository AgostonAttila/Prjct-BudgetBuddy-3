using System.Text.Json;
using BudgetBuddy.Shared.Infrastructure.Logging;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace BudgetBuddy.Shared.Infrastructure.Extensions;

public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder, string serviceName)
    {
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            var piiMaskingEnabled = !context.HostingEnvironment.IsDevelopment();
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.WithProperty("ServiceName", serviceName)
                .Enrich.With(new PiiMaskingEnricher(piiMaskingEnabled))
                .Destructure.With(new SensitiveDataDestructuringPolicy());
        });

        var otlpEndpoint = builder.Configuration.GetValue<string>("OpenTelemetry:OtlpEndpoint")
            ?? "http://localhost:4317";

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName, serviceVersion: "1.0.0"))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(opt => opt.RecordException = true)
                    .AddHttpClientInstrumentation(opt => opt.RecordException = true)
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddSource("BudgetBuddy.Kafka")
                    .AddOtlpExporter(opt => opt.Endpoint = new Uri(otlpEndpoint));

                // Finding #13: configurable head-based sampling so Jaeger isn't flooded in production.
                // Dev keeps AlwaysOn for full trace visibility; prod defaults to 10 % (configurable).
                // ParentBased honours the sampling decision propagated from an upstream service, so
                // traces that originate externally are always sampled consistently end-to-end.
                if (builder.Environment.IsDevelopment())
                {
                    tracing.SetSampler(new AlwaysOnSampler());
                    tracing.AddConsoleExporter();
                }
                else
                {
                    var ratio = builder.Configuration.GetValue<double>("OpenTelemetry:SamplingRatio", 0.1);
                    tracing.SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(ratio)));
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("BudgetBuddy.Kafka")
                    .AddMeter("BudgetBuddy.Handlers")   // finding #12: handler duration + invocations
                    .AddPrometheusExporter();
            });

        // Health checks (basic — each service adds its own DbContext check after calling AddModuleDbContext)
        builder.Services.AddHealthChecks();

        return builder;
    }

    public static void MapObservabilityEndpoints(this WebApplication app)
    {
        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (ctx, report) =>
            {
                // O-02: detailed health response — includes description, tags, and error message
                // so monitoring tools can surface actionable info without manual log correlation.
                ctx.Response.ContentType = "application/json";
                var result = new
                {
                    status          = report.Status.ToString(),
                    totalDurationMs = report.TotalDuration.TotalMilliseconds,
                    checks          = report.Entries.Select(e => new
                    {
                        name        = e.Key,
                        status      = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        durationMs  = e.Value.Duration.TotalMilliseconds,
                        tags        = e.Value.Tags,
                        error       = e.Value.Exception?.Message,
                    })
                };
                await ctx.Response.WriteAsync(JsonSerializer.Serialize(result, jsonOptions));
            }
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
        });

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        });

        app.MapPrometheusScrapingEndpoint();
    }
}

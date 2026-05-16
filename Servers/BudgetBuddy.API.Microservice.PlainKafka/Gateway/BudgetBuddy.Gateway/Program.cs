using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, _, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithProperty("ServiceName", "api-gateway")
    .WriteTo.Console()
    .WriteTo.Seq(builder.Configuration["Seq:ServerUrl"] ?? string.Empty));

// S-09: Fail fast if Keycloak Authority is missing in production
var keycloakAuthority = builder.Configuration["Keycloak:Authority"];
if (string.IsNullOrWhiteSpace(keycloakAuthority) && builder.Environment.IsProduction())
{
    throw new InvalidOperationException(
        "Keycloak:Authority must be configured in production. " +
        "Set it via environment variable KEYCLOAK__AUTHORITY or Azure Key Vault.");
}

// Keycloak JWT validation at gateway level (defense in depth)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.Authority            = builder.Configuration["Keycloak:Authority"];
        opt.Audience             = builder.Configuration["Keycloak:Audience"] ?? "budgetbuddy-api";
        opt.RequireHttpsMetadata = builder.Configuration.GetValue<bool>("Keycloak:RequireHttps");
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer   = true,
            ValidateAudience = true,
            NameClaimType    = "preferred_username",
            RoleClaimType    = "realm_access.roles",
        };
        opt.MapInboundClaims = false;
    });

builder.Services.AddAuthorization();

// CORS — enforce whitelist at the gateway (the TLS termination point)
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("Default", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .WithMethods("GET", "POST", "PUT", "DELETE")
            .WithHeaders("Content-Type", "Authorization", "X-Correlation-ID")
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromHours(1));
    });
});

// YARP
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// OpenTelemetry
var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://jaeger:4317";
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("api-gateway", serviceVersion: "1.0.0"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint)))
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter());

builder.Services.AddHealthChecks();

// S-06: HSTS — the gateway is the TLS termination point; all downstreams are internal HTTP.
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddHsts(opt =>
    {
        opt.MaxAge           = TimeSpan.FromDays(365);
        opt.IncludeSubDomains = true;
        opt.Preload          = true;
    });
}

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("Default");
app.UseAuthentication();
app.UseAuthorization();

// Propagate CorrelationId
app.Use(async (ctx, next) =>
{
    var correlationId = ctx.Request.Headers["X-Correlation-ID"].FirstOrDefault()
        ?? Guid.NewGuid().ToString();
    ctx.Response.Headers["X-Correlation-ID"] = correlationId;
    using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
    {
        await next();
    }
});

// Item 13: API version enforcement at gateway level
// Injects a default api-version=1.0 header when absent so upstream services
// (which have AssumeDefaultVersionWhenUnspecified=true) behave consistently.
// Requests explicitly targeting a future version are forwarded as-is.
app.Use(async (ctx, next) =>
{
    const string queryKey  = "api-version";
    const string headerKey = "X-Api-Version";
    const string defaultVersion = "1.0";

    // Skip for Keycloak/health routes
    var path = ctx.Request.Path.Value ?? string.Empty;
    if (!path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
    {
        await next();
        return;
    }

    var hasQuery  = ctx.Request.Query.ContainsKey(queryKey);
    var hasHeader = ctx.Request.Headers.ContainsKey(headerKey);

    if (!hasQuery && !hasHeader)
    {
        // Inject default so upstream versioning middleware sees it
        ctx.Request.Headers[headerKey] = defaultVersion;
        ctx.Response.Headers["X-Api-Version"] = defaultVersion;
        Log.Debug("Gateway: no api-version supplied, defaulting to {Version}", defaultVersion);
    }
    else
    {
        var version = hasQuery
            ? ctx.Request.Query[queryKey].FirstOrDefault()
            : ctx.Request.Headers[headerKey].FirstOrDefault();
        ctx.Response.Headers["X-Api-Version"] = version;
    }

    await next();
});

app.MapPrometheusScrapingEndpoint();
app.MapHealthChecks("/health");
app.MapReverseProxy();

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "api-gateway terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

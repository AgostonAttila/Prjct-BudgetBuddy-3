using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence.Extensions;
using BudgetBuddy.API.VSA.Common.Infrastructure.Security.Authentication;
using BudgetBuddy.API.VSA.Features.BudgetAlerts.Jobs;
using BudgetBuddy.API.VSA.Features.MarketData.Jobs;
using BudgetBuddy.API.VSA.Features.MarketData.Services;
using Serilog;

// Bootstrap logger - minimal configuration for startup errors only
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();


// Build the application outside of try-catch so that WebApplicationFactory
// (integration tests) can intercept startup exceptions properly.
// If the build itself throws, it must propagate — otherwise WebApplicationFactory
// sees "entry point exited without building IHost" and hides the real error.
var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddConnectionStringConfiguration(builder.Environment);

builder.Services.AddTypedConfigurations(builder);

builder.AddObservability();
builder.AddSecurity(builder.Configuration);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiServices(builder.Configuration);
builder.Services.AddAuthenticationServices(builder.Configuration);
builder.Services.AddCaching(builder.Configuration, builder.Environment);
builder.Services.AddCompression();

builder.Services.AddApplicationServices();
builder.Services.AddBackgroundJobs(builder.Configuration, q =>
{
    q.AddBudgetAlertJob(builder.Configuration);
    q.AddMarketDataJobs(builder.Configuration);
});

var app = builder.Build();

// Validate required configuration now that Build() has finalized all config sources,
// including WebApplicationFactory test overrides (ConfigureAppConfiguration).
// Both JWT and connection string validation must run here — NOT during service registration —
// because test config overrides only land after Build().
ApiExtensions.ValidateConfigurations(app.Configuration);

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
    ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
    ?? "Production";
JwtConfigurationValidator.ValidateSecretKey(app.Configuration["Jwt:SecretKey"], environment);

// Configure middleware pipeline
app.UseMiddlewarePipeline();
app.UseSecurity();
app.UseAuthenticationServices();

// Map endpoints
app.MapObservabilityEndpoints();
app.MapApiEndpoints();
app.MapAuthenticationEndpoints();

// Run the application — wrap only the runtime in try-catch
try
{
    await app.MigrateDatabaseAsync();

    // Backfill historical market data in the background (non-blocking).
    // Detects gaps from the earliest investment/transaction date and fills them via
    // Frankfurter (FX) and Alpha Vantage (prices). Safe to run on every startup.
    _ = Task.Run(async () =>
    {
        await using var scope = app.Services.CreateAsyncScope();
        var backfill = scope.ServiceProvider.GetRequiredService<IMarketDataBackfillService>();
        var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        try
        {
            await backfill.BackfillAllMissingAsync();
        }
        catch (Exception ex)
        {
            startupLogger.LogError(ex, "Startup market data backfill failed");
        }
    });

    // Seed database with sample data (only in Development)
    if (app.Environment.IsDevelopment() && args.Contains("--seed"))
    {
        await app.Services.SeedDatabaseAsync();
        Log.Information("Database seeded with sample data");
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Required for WebApplicationFactory<Program> in integration tests
public partial class Program { }































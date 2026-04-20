using BudgetBuddy.API.ModularMonolith.Seeders;
using BudgetBuddy.Shared.Infrastructure.Extensions;
using BudgetBuddy.Shared.Infrastructure.Persistence.Extensions;
using BudgetBuddy.Shared.Infrastructure.Persistence.Seeders;
using BudgetBuddy.Module.Auth.Infrastructure.Authentication;
using BudgetBuddy.Module.Auth.Persistence;
using BudgetBuddy.Module.Accounts.Persistence;
using BudgetBuddy.Module.Transactions.Persistence;
using BudgetBuddy.Module.Budgets.Persistence;
using BudgetBuddy.Module.Investments.Persistence;
using BudgetBuddy.Module.ReferenceData.Persistence;
using BudgetBuddy.Shared.Contracts;
using Microsoft.EntityFrameworkCore;
using BudgetBuddy.Module.Auth;
using BudgetBuddy.Module.Accounts;
using BudgetBuddy.Module.Transactions;
using BudgetBuddy.Module.Budgets;
using BudgetBuddy.Module.Investments;
using BudgetBuddy.Module.Analytics;
using BudgetBuddy.Module.ReferenceData;
using BudgetBuddy.Module.Budgets.Jobs;
using BudgetBuddy.Module.Investments.Jobs;
using BudgetBuddy.Module.Transactions.Features.Transactions.Jobs;
using BudgetBuddy.Module.Investments.Features.MarketData.Services;
using Carter;
using Serilog;

// Bootstrap logger
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

// ── Infrastructure setup (from Shared.Infrastructure) ───────────────────────
builder.Configuration.AddConnectionStringConfiguration(builder.Environment);
builder.Services.AddTypedConfigurations(builder);
builder.AddObservability();
builder.AddSecurity(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiServices(builder.Configuration,
    typeof(BudgetBuddy.Module.Auth.AuthModule).Assembly,
    typeof(BudgetBuddy.Module.Accounts.AccountsModule).Assembly,
    typeof(BudgetBuddy.Module.Transactions.TransactionsModule).Assembly,
    typeof(BudgetBuddy.Module.Budgets.BudgetsModule).Assembly,
    typeof(BudgetBuddy.Module.Investments.InvestmentsModule).Assembly,
    typeof(BudgetBuddy.Module.Analytics.AnalyticsModule).Assembly,
    typeof(BudgetBuddy.Module.ReferenceData.ReferenceDataModule).Assembly
);

// ── Module registration — BEFORE AddAuthenticationServices ──────────────────
// AuthModule.RegisterServices() registers Identity (AddIdentityApiEndpoints),
// so it must run before AddAuthenticationServices (which only registers the scheme/policies).
IModule[] modules =
[
    new AuthModule(),
    new AccountsModule(),
    new TransactionsModule(),
    new BudgetsModule(),
    new InvestmentsModule(),
    new AnalyticsModule(),
    new ReferenceDataModule(),
];

foreach (var module in modules)
    module.RegisterServices(builder.Services, builder.Configuration);

builder.Services.AddAuthenticationServices(builder.Configuration);
builder.Services.AddCaching(builder.Configuration, builder.Environment);
builder.Services.AddCompression();
builder.Services.AddApplicationServices();

// Demo data seeder lives in Host to avoid circular dependency (Shared.Infrastructure ← Modules)
builder.Services.AddScoped<ISeeder, Seeder>();

// ── Background jobs ──────────────────────────────────────────────────────────
builder.Services.AddBackgroundJobs(builder.Configuration, q =>
{
    q.AddBudgetAlertJob(builder.Configuration);
    q.AddMarketDataJobs(builder.Configuration);
    q.AddTransactionsOutboxProcessorJob(builder.Configuration);
});

// ── Build ────────────────────────────────────────────────────────────────────
var app = builder.Build();

ValidateJWTSecret(app);

// ── Middleware pipeline ──────────────────────────────────────────────────────
app.UseMiddlewarePipeline();
app.UseSecurity();
app.UseAuthenticationServices();

// ── Endpoints ────────────────────────────────────────────────────────────────
app.MapObservabilityEndpoints();
app.MapApiEndpoints();

foreach (var module in modules)
    module.MapEndpoints(app);

// ── Run ──────────────────────────────────────────────────────────────────────
try
{
    await app.MigrateDatabaseAsync();                         // AppDbContext (AuditLogs)
    await app.MigrateDatabaseAsync<AuthDbContext>();          // Identity + RefreshTokens + SecurityEvents
    await app.MigrateDatabaseAsync<AccountsDbContext>();
    await app.MigrateDatabaseAsync<TransactionsDbContext>();
    await app.MigrateDatabaseAsync<BudgetsDbContext>();
    await app.MigrateDatabaseAsync<InvestmentsDbContext>();
    await app.MigrateDatabaseAsync<ReferenceDataDbContext>();
    BackFillMarketDataAsync(app);

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

return;

void ValidateJWTSecret(WebApplication webApplication)
{
    ApiExtensions.ValidateConfigurations(webApplication.Configuration);
    var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                      ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                      ?? "Production";
    JwtConfigurationValidator.ValidateSecretKey(webApplication.Configuration["Jwt:SecretKey"], environment);
}

void BackFillMarketDataAsync(WebApplication webApp)
{
    _ = Task.Run(async () =>
    {
        await using var scope = webApp.Services.CreateAsyncScope();
        // IMarketDataBackfillService is registered by InvestmentsModule
        var backfill = scope.ServiceProvider.GetService<IMarketDataBackfillService>();
        if (backfill is null) return;
        var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        try { await backfill.BackfillAllMissingAsync(); }
        catch (Exception ex) { startupLogger.LogError(ex, "Startup market data backfill failed"); }
    });
}

public partial class Program { }

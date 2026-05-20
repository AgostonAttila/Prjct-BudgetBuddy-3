using BudgetBuddy.Service.Accounts.Messaging;
using BudgetBuddy.Service.Accounts.ReadModels;
using BudgetBuddy.Shared.Infrastructure.Persistence.Extensions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddObservability("accounts-service");
    builder.AddSecurity(builder.Configuration);
    builder.Services.AddKeycloakAuthentication(builder.Configuration);
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApiServices(builder.Configuration, typeof(Program).Assembly);
    builder.Services.AddModuleDbContext<AccountsDbContext>(builder.Configuration);
    builder.Services.AddDbHealthCheck<AccountsDbContext>();
    builder.Services.AddCaching(builder.Configuration, builder.Environment);
    builder.Services.AddCompression();
    builder.Services.AddBackgroundJobs(builder.Configuration, q =>
    {
        // AccountsOutboxProcessorJob removed — Wolverine polling agent handles outbox delivery
        q.AddAuditLogRetentionJob<AccountsDbContext>(builder.Configuration);
    });
    builder.Services.AddApplicationServices();
    builder.Services.AddCurrencyConversionService(builder.Configuration);
    builder.Services.AddScoped<IAccountTransactionTotalsRepository, AccountTransactionTotalsRepository>();
    builder.Services.AddScoped<IAccountTransactionSummary, AccountTransactionSummaryService>();

    builder.AddWolverine(typeof(Program).Assembly, opts => opts.AddAccountsMessaging(builder.Configuration));

    var app = builder.Build();

    app.UseMiddlewarePipeline();
    app.UseSecurity();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapObservabilityEndpoints();
    app.MapApiEndpoints();

    await app.MigrateDatabaseAsync<AccountsDbContext>();
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "accounts-service terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

using BudgetBuddy.Service.Analytics.Features.Dashboard.Services;
using BudgetBuddy.Service.Analytics.Features.Reports.Services;
using BudgetBuddy.Service.Analytics.Messaging;
using BudgetBuddy.Service.Analytics.Persistence;
using BudgetBuddy.Service.Analytics.ReadModels;
using BudgetBuddy.Shared.Infrastructure.Extensions;
using BudgetBuddy.Shared.Infrastructure.Persistence.Extensions;
using BudgetBuddy.Shared.Messages.Contracts.Accounts;
using BudgetBuddy.Shared.Messages.Contracts.Budgets;
using BudgetBuddy.Shared.Messages.Contracts.Financial;
using BudgetBuddy.Shared.Messages.Contracts.Investments;
using BudgetBuddy.Shared.Messages.Contracts.ReferenceData;
using BudgetBuddy.Shared.Messages.Contracts.Transactions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability("analytics-service");
builder.AddSecurity(builder.Configuration);
builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiServices(builder.Configuration, typeof(Program).Assembly);
builder.Services.AddModuleDbContext<AnalyticsDbContext>(builder.Configuration);
// Register factory so DashboardService can create per-task contexts for parallel queries (P-04)
builder.Services.AddDbContextFactory<AnalyticsDbContext>(lifetime: ServiceLifetime.Scoped);
builder.Services.AddDbHealthCheck<AnalyticsDbContext>();
builder.Services.AddCaching(builder.Configuration, builder.Environment);
builder.Services.AddCompression();
builder.Services.AddBackgroundJobs(builder.Configuration, q =>
{
    q.AddAuditLogRetentionJob<AnalyticsDbContext>(builder.Configuration);
});
builder.Services.AddApplicationServices();
builder.Services.AddCurrencyConversionService(builder.Configuration);
builder.Services.AddFinancialServiceClient(builder.Configuration);  // live market prices via Financial service HTTP

// Read model repositories
builder.Services.AddScoped<IAccountSnapshotRepository, AccountSnapshotRepository>();
builder.Services.AddScoped<ICategorySnapshotRepository, CategorySnapshotRepository>();
builder.Services.AddScoped<ITransactionReadModelRepository, TransactionReadModelRepository>();
builder.Services.AddScoped<IBudgetReadModelRepository, BudgetReadModelRepository>();
builder.Services.AddScoped<IInvestmentReadModelRepository, InvestmentReadModelRepository>();

// Cross-module service implementations (backed by local read models)
builder.Services.AddScoped<IAccountOwnershipService, AccountOwnershipService>();
builder.Services.AddScoped<IAccountBalanceService, AccountBalanceService>();
builder.Services.AddScoped<IAccountTransactionSummary, AccountTransactionSummaryService>();
builder.Services.AddScoped<ICategoryQueryService, CategoryQueryService>();
builder.Services.AddScoped<ITransactionQueryService, TransactionQueryService>();
builder.Services.AddScoped<IBudgetQueryService, BudgetQueryService>();
builder.Services.AddScoped<IInvestmentDataService, InvestmentDataService>();
builder.Services.AddScoped<IInvestmentCalculationService, InvestmentCalculationService>();

builder.Services.AddScoped<IUserCurrencyService, UserCurrencyService>();

// Analytics feature services
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IIncomeExpenseReportService>(sp => sp.GetRequiredService<IReportService>());
builder.Services.AddScoped<IMonthlySummaryReportService>(sp => sp.GetRequiredService<IReportService>());
builder.Services.AddScoped<ISpendingReportService>(sp => sp.GetRequiredService<IReportService>());
builder.Services.AddScoped<IInvestmentReportService>(sp => sp.GetRequiredService<IReportService>());

builder.AddWolverine(typeof(Program).Assembly, opts => opts.AddAnalyticsMessaging(builder.Configuration));

var app = builder.Build();

app.UseMiddlewarePipeline();
app.UseSecurity();
app.UseAuthentication();
app.UseAuthorization();

// M-02: Inform clients how fresh the read-model data is.
// Analytics reads from event-sourced projections; the header helps
// clients decide whether to show a staleness warning.
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/api"))
    {
        ctx.Response.Headers["X-Data-As-Of"] = DateTime.UtcNow.ToString("O");
    }

    await next();
});
app.MapObservabilityEndpoints();
app.MapApiEndpoints();

try
{
    await app.MigrateDatabaseAsync<AnalyticsDbContext>();
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "analytics-service terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

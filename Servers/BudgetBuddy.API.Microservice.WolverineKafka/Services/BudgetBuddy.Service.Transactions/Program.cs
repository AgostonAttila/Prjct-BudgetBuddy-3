using BudgetBuddy.Service.Transactions.Features.Transactions.Jobs;
using BudgetBuddy.Service.Transactions.Features.Transactions.Services;
using BudgetBuddy.Service.Transactions.Features.Transfers.Services;
using BudgetBuddy.Service.Transactions.Messaging;
using BudgetBuddy.Service.Transactions.Persistence;
using BudgetBuddy.Service.Transactions.ReadModels;
using BudgetBuddy.Shared.Infrastructure.Extensions;
using BudgetBuddy.Shared.Infrastructure.Persistence.Extensions;
using Carter;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability("transactions-service");
builder.AddSecurity(builder.Configuration);
builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiServices(builder.Configuration, typeof(Program).Assembly);
builder.Services.AddModuleDbContext<TransactionsDbContext>(builder.Configuration);
builder.Services.AddDbHealthCheck<TransactionsDbContext>();
builder.Services.AddCaching(builder.Configuration, builder.Environment);
builder.Services.AddCompression();
builder.Services.AddBackgroundJobs(builder.Configuration, q =>
{
    // Phase 1: TransactionsOutboxProcessorJob eltávolítva — Wolverine polling agent kezeli
    q.AddSagaTimeoutJob(builder.Configuration);
    q.AddAuditLogRetentionJob<TransactionsDbContext>(builder.Configuration);
});
builder.Services.AddApplicationServices();
builder.Services.AddCurrencyConversionService(builder.Configuration);
builder.Services.AddScoped<ITransferService, TransferService>();
builder.Services.AddScoped<IDataImportService, ExcelImportService>();
builder.Services.AddScoped<ITransactionSearchService, TransactionSearchService>();
builder.Services.AddScoped<ITransactionValidationService, TransactionValidationService>();
builder.Services.AddScoped<IAccountSnapshotRepository, AccountSnapshotRepository>();
builder.Services.AddScoped<IAccountSnapshotService, AccountSnapshotService>();
builder.Services.AddScoped<IAccountOwnershipService, AccountOwnershipService>();
builder.Services.AddScoped<BudgetBuddy.Service.Transactions.ReadModels.ICategorySnapshotRepository, BudgetBuddy.Service.Transactions.ReadModels.CategorySnapshotRepository>();
builder.Services.AddScoped<ICategoryQueryService, BudgetBuddy.Service.Transactions.ReadModels.CategoryQueryService>();
builder.Services.AddReadOnlyDbContext<TransactionsDbContext>();  // Item 10: CQRS read side

builder.AddWolverine(typeof(Program).Assembly, opts => opts.AddTransactionsMessaging(builder.Configuration));

var app = builder.Build();

app.UseMiddlewarePipeline();
app.UseSecurity();
app.UseAuthentication();
app.UseAuthorization();
app.MapObservabilityEndpoints();
app.MapApiEndpoints();

try
{
    await app.MigrateDatabaseAsync<TransactionsDbContext>();
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "transactions-service terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

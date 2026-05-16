using BudgetBuddy.Service.Transactions.Features.Transactions.Jobs;
using BudgetBuddy.Service.Transactions.Features.Transactions.Services;
using BudgetBuddy.Service.Transactions.Features.Transfers.Services;
using BudgetBuddy.Service.Transactions.Persistence;
using BudgetBuddy.Service.Transactions.ReadModels;
using BudgetBuddy.Shared.Infrastructure.Extensions;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Infrastructure.Persistence.Extensions;
using BudgetBuddy.Shared.Messages.Contracts.Accounts;
using BudgetBuddy.Shared.Messages.Topics;
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
builder.Services.AddKafkaMessaging(builder.Configuration);
builder.Services.AddKafkaTopicValidation(
    TopicNames.TransactionsCreated,
    TopicNames.TransactionsUpdated,
    TopicNames.TransactionsDeleted,
    TopicNames.TransactionsDlq,
    TopicNames.AccountsDlq,
    TopicNames.ReferenceDataDlq,
    TopicNames.TransactionsReversed,
    TopicNames.AccountsChangelog,
    TopicNames.AccountsBalanceReserved,
    TopicNames.CategoryChanged);
builder.Services.AddApiServices(builder.Configuration, typeof(Program).Assembly);
builder.Services.AddModuleDbContext<TransactionsDbContext>(builder.Configuration);
builder.Services.AddDbHealthCheck<TransactionsDbContext>();
builder.Services.AddCaching(builder.Configuration, builder.Environment);
builder.Services.AddCompression();
builder.Services.AddBackgroundJobs(builder.Configuration, q =>
{
    q.AddTransactionsOutboxProcessorJob(builder.Configuration);
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
builder.Services.AddHostedService<BudgetBuddy.Service.Transactions.Messaging.Consumers.AccountChangelogConsumer>();
builder.Services.AddHostedService<BudgetBuddy.Service.Transactions.Messaging.Consumers.CategoryChangedConsumer>();
builder.Services.AddHostedService<BudgetBuddy.Service.Transactions.Messaging.Consumers.AccountBalanceSagaConsumer>(); // Item 9: saga coordinator
builder.Services.AddReadOnlyDbContext<TransactionsDbContext>();  // Item 10: CQRS read side

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

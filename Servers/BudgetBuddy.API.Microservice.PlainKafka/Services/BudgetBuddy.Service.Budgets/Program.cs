using BudgetBuddy.Service.Budgets.Features.BudgetAlerts.Services;
using BudgetBuddy.Service.Budgets.Jobs;
using BudgetBuddy.Service.Budgets.ReadModels;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Infrastructure.Persistence.Extensions;
using BudgetBuddy.Shared.Messages.Topics;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability("budgets-service");
builder.AddSecurity(builder.Configuration);
builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddKafkaMessaging(builder.Configuration);
builder.Services.AddKafkaTopicValidation(
    TopicNames.CategoryChanged,
    TopicNames.TransactionsCreated,
    TopicNames.TransactionsUpdated,
    TopicNames.TransactionsDeleted,
    TopicNames.BudgetsCreated,
    TopicNames.BudgetsUpdated,
    TopicNames.BudgetsDeleted,
    TopicNames.BudgetsDlq,
    TopicNames.ReferenceDataDlq,
    TopicNames.TransactionsDlq);
builder.Services.AddApiServices(builder.Configuration, typeof(Program).Assembly);
builder.Services.AddModuleDbContext<BudgetsDbContext>(builder.Configuration);
builder.Services.AddDbHealthCheck<BudgetsDbContext>();
builder.Services.AddCaching(builder.Configuration, builder.Environment);
builder.Services.AddCompression();
builder.Services.AddBackgroundJobs(builder.Configuration, q =>
{
    q.AddBudgetAlertJob(builder.Configuration);
    q.AddBudgetsOutboxProcessorJob(builder.Configuration);
    q.AddAuditLogRetentionJob<BudgetsDbContext>(builder.Configuration);
});
builder.Services.AddApplicationServices();
builder.Services.AddCurrencyConversionService(builder.Configuration);
builder.Services.AddScoped<IBudgetAlertService, BudgetAlertService>();
builder.Services.AddScoped<IBudgetAlertCalculationService>(sp => sp.GetRequiredService<IBudgetAlertService>());
builder.Services.AddScoped<ICategorySnapshotRepository, CategorySnapshotRepository>();
builder.Services.AddScoped<ICategoryQueryService, CategoryQueryService>();
builder.Services.AddScoped<ICategorySpendingRepository, CategorySpendingRepository>();
builder.Services.AddScoped<ITransactionQueryService, TransactionQueryService>();
builder.Services.AddHostedService<BudgetBuddy.Service.Budgets.Messaging.Consumers.CategoryChangedConsumer>();
builder.Services.AddHostedService<BudgetBuddy.Service.Budgets.Messaging.Consumers.TransactionSpendingConsumer>();

var app = builder.Build();

app.UseMiddlewarePipeline();
app.UseSecurity();
app.UseAuthentication();
app.UseAuthorization();
app.MapObservabilityEndpoints();
app.MapApiEndpoints();

try
{
    await app.MigrateDatabaseAsync<BudgetsDbContext>();
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "budgets-service terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

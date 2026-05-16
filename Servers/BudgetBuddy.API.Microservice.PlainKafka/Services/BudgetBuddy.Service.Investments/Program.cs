using BudgetBuddy.Service.Investments.Features.Investments.Services;
using BudgetBuddy.Service.Investments.Features.MarketData.Services;
using BudgetBuddy.Service.Investments.Features.Services;
using BudgetBuddy.Service.Investments.Services;
using BudgetBuddy.Service.Investments.Jobs;
using BudgetBuddy.Service.Investments.ReadModels;
using BudgetBuddy.Shared.Infrastructure.Extensions;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Infrastructure.Persistence.Extensions;
using BudgetBuddy.Shared.Messages.Contracts;
using BudgetBuddy.Shared.Messages.Contracts.Accounts;
using BudgetBuddy.Shared.Messages.Contracts.Investments;
using BudgetBuddy.Shared.Messages.Contracts.Transactions;
using BudgetBuddy.Shared.Messages.Topics;
using Carter;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability("investments-service");
builder.AddSecurity(builder.Configuration);
builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddKafkaMessaging(builder.Configuration);
builder.Services.AddKafkaTopicValidation(
    TopicNames.MarketPriceUpdated,
    TopicNames.InvestmentsDlq,
    TopicNames.AccountsDlq,
    TopicNames.AccountsChangelog,
    TopicNames.InvestmentsCreated,
    TopicNames.InvestmentsUpdated,
    TopicNames.InvestmentsDeleted);
builder.Services.AddApiServices(builder.Configuration, typeof(Program).Assembly);
builder.Services.AddModuleDbContext<InvestmentsDbContext>(builder.Configuration);
builder.Services.AddDbHealthCheck<InvestmentsDbContext>();
builder.Services.AddCaching(builder.Configuration, builder.Environment);
builder.Services.AddCompression();
builder.Services.AddBackgroundJobs(builder.Configuration, q =>
{
    q.AddMarketDataJobs(builder.Configuration);
    q.AddInvestmentsOutboxProcessorJob(builder.Configuration);
    q.AddAuditLogRetentionJob<InvestmentsDbContext>(builder.Configuration);
});
builder.Services.AddApplicationServices();
builder.Services.AddCurrencyConversionService(builder.Configuration);  // Frankfurter FX — FX rate conversion
builder.Services.AddFinancialServiceClient(builder.Configuration);    // IPriceService + IHistoricalPriceService → Financial microservice
builder.Services.AddScoped<IMarketDataBackfillService, MarketDataBackfillService>();
builder.Services.AddScoped<IAccountSnapshotRepository, AccountSnapshotRepository>();
builder.Services.AddScoped<IAccountOwnershipService, AccountOwnershipService>();
builder.Services.AddScoped<IAccountBalanceService, AccountBalanceService>();
builder.Services.AddScoped<IUserCurrencyService, UserCurrencyService>();
builder.Services.AddScoped<ITransactionQueryService, TransactionQueryServiceStub>();
builder.Services.AddScoped<IInvestmentCalculationService, InvestmentCalculationService>();
builder.Services.AddScoped<IInvestmentDataService, InvestmentDataService>();
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddHostedService<BudgetBuddy.Service.Investments.Messaging.Consumers.AccountChangelogConsumer>();

var app = builder.Build();

app.UseMiddlewarePipeline();
app.UseSecurity();
app.UseAuthentication();
app.UseAuthorization();
app.MapObservabilityEndpoints();
app.MapApiEndpoints();

try
{
    await app.MigrateDatabaseAsync<InvestmentsDbContext>();
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "investments-service terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

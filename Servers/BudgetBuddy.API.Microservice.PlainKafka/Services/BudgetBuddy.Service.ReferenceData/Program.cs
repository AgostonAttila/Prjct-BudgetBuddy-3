using BudgetBuddy.Shared.Infrastructure.Extensions;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Infrastructure.Persistence.Extensions;
using BudgetBuddy.Shared.Messages.Topics;
using Carter;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability("referencedata-service");
builder.AddSecurity(builder.Configuration);
builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddKafkaMessaging(builder.Configuration);
builder.Services.AddKafkaTopicValidation(
    TopicNames.CategoryChanged,
    TopicNames.ReferenceDataDlq,
    TopicNames.AccountsDlq,
    TopicNames.AccountsChangelog);
builder.Services.AddApiServices(builder.Configuration, typeof(Program).Assembly);

builder.Services.AddModuleDbContext<ReferenceDataDbContext>(builder.Configuration);
builder.Services.AddDbHealthCheck<ReferenceDataDbContext>();
builder.Services.AddCaching(builder.Configuration, builder.Environment);
builder.Services.AddCompression();
builder.Services.AddBackgroundJobs(builder.Configuration, q =>
{
    q.AddAuditLogRetentionJob<ReferenceDataDbContext>(builder.Configuration);
});
builder.Services.AddApplicationServices();
builder.Services.AddScoped<BudgetBuddy.Service.ReferenceData.ReadModels.IAccountSnapshotRepository, BudgetBuddy.Service.ReferenceData.ReadModels.AccountSnapshotRepository>();
builder.Services.AddScoped<IAccountOwnershipService, BudgetBuddy.Service.ReferenceData.ReadModels.AccountOwnershipService>();
builder.Services.AddHostedService<BudgetBuddy.Service.ReferenceData.Messaging.Consumers.AccountChangelogConsumer>();

var app = builder.Build();

app.UseMiddlewarePipeline();
app.UseSecurity();
app.UseAuthentication();
app.UseAuthorization();
app.MapObservabilityEndpoints();
app.MapApiEndpoints();

try
{
    await app.MigrateDatabaseAsync<ReferenceDataDbContext>();
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "referencedata-service terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

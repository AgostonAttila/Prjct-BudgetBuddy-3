using BudgetBuddy.Shared.Infrastructure.Extensions;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Infrastructure.Notification.Email;
using BudgetBuddy.Shared.Messages.Topics;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability("notifications-service");
builder.Services.AddKafkaMessaging(builder.Configuration);
builder.Services.AddKafkaTopicValidation(
    TopicNames.BudgetAlertTriggered,
    TopicNames.BudgetsDlq);
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection(EmailSettings.SectionName));
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddHostedService<BudgetBuddy.Service.Notifications.Messaging.Consumers.BudgetAlertConsumer>();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapObservabilityEndpoints();

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "notifications-service terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

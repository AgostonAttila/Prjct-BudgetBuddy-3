using BudgetBuddy.Service.Notifications.Messaging;
using BudgetBuddy.Shared.Infrastructure.Extensions;
using BudgetBuddy.Shared.Infrastructure.Notification.Email;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability("notifications-service");
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection(EmailSettings.SectionName));
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddHealthChecks();

builder.AddWolverine(typeof(Program).Assembly, opts => opts.AddNotificationsMessaging(builder.Configuration));

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

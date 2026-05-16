using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Infrastructure.Notification.Email;
using BudgetBuddy.Shared.Messages.Events.Budgets;
using BudgetBuddy.Shared.Messages.Topics;

namespace BudgetBuddy.Service.Notifications.Messaging.Consumers;

/// <summary>
/// Consumes budget alert events and sends email notifications.
/// </summary>
public sealed
    class BudgetAlertConsumer(
    KafkaSettings settings,
    IServiceScopeFactory scopeFactory,
    ILogger<BudgetAlertConsumer> logger)
    : KafkaConsumerBase<BudgetAlertTriggeredEvent>(
        settings,
        TopicNames.BudgetAlertTriggered,
        ConsumerGroups.NotificationsBudgets,
        scopeFactory,
        logger)
{
    protected override string? DlqTopic => TopicNames.BudgetsDlq;

    protected override async Task ProcessAsync(
        BudgetAlertTriggeredEvent evt, IServiceProvider services, CancellationToken ct)
    {
        if (evt.UserEmail is null)
        {
            return;
        }

        var emailService = services.GetRequiredService<IEmailService>();
        var message = EmailTemplates.CreateBudgetAlertEmail(
            evt.UserEmail,
            evt.UserEmail,
            evt.BudgetName,
            evt.CurrentSpending,
            evt.Limit);

        await emailService.SendEmailAsync(message, ct);

        Logger.LogInformation("Sent budget alert email for budget {BudgetId}", evt.BudgetId);
    }
}

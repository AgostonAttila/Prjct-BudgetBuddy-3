using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Infrastructure.Notification.Email;
using BudgetBuddy.Shared.Messages.Events.Budgets;

namespace BudgetBuddy.Service.Notifications.Messaging.Handlers;

/// <summary>
/// Sends email notification when a budget alert is triggered.
/// Replaces <c>BudgetAlertConsumer</c> (KafkaConsumerBase).
/// </summary>
[SupportedSchemaVersions("1")]
public class BudgetAlertHandler
{
    protected BudgetAlertHandler() { }

    public static async Task Handle(
        BudgetAlertTriggeredEvent evt,
        IEmailService emailService,
        ILogger<BudgetAlertHandler> logger,
        CancellationToken ct)
    {
        if (evt.UserEmail is null)
        {
            return;
        }

        var message = EmailTemplates.CreateBudgetAlertEmail(
            evt.UserEmail,
            evt.UserEmail,
            evt.BudgetName,
            evt.CurrentSpending,
            evt.Limit);

        await emailService.SendEmailAsync(message, ct);

        logger.LogInformation("Sent budget alert email for budget {BudgetId}", evt.BudgetId);
    }
}

namespace BudgetBuddy.Shared.Infrastructure.Notification.Email;

/// <summary>
/// Service for sending emails via SMTP
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email asynchronously
    /// </summary>
    Task<bool> SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends multiple emails asynchronously (batched)
    /// </summary>
    Task<int> SendBatchEmailAsync(IEnumerable<EmailMessage> messages, CancellationToken cancellationToken = default);
}

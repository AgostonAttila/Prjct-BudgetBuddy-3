using BudgetBuddy.API.VSA.Common.Infrastructure.Notification.Email;

namespace BudgetBuddy.API.VSA.Common.Infrastructure.Notification;

/// <summary>
/// Service for sending emails via SMTP
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email asynchronously
    /// </summary>
    /// <param name="message">The email message to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if sent successfully, false otherwise</returns>
    Task<bool> SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends multiple emails asynchronously (batched)
    /// </summary>
    /// <param name="messages">List of email messages to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of successfully sent emails</returns>
    Task<int> SendBatchEmailAsync(IEnumerable<EmailMessage> messages, CancellationToken cancellationToken = default);
}

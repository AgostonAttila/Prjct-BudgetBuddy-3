using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;


namespace BudgetBuddy.API.VSA.Common.Infrastructure.Notification.Email;

/// <summary>
/// Email service implementation using MailKit
/// </summary>
public class EmailService(
    IOptions<EmailSettings> settings,
    ILogger<EmailService> logger)
    : IEmailService
{
    private readonly EmailSettings _settings = settings.Value;

    public async Task<bool> SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            logger.LogWarning("Email service is disabled. Email not sent to {To}", message.To);
            return false;
        }

        try
        {
            var mimeMessage = CreateMimeMessage(message);

            using var client = new SmtpClient();

            // Connect to SMTP server
            await client.ConnectAsync(
                _settings.SmtpHost,
                _settings.SmtpPort,
                _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
                cancellationToken);

            // Authenticate
            await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);

            // Send email
            await client.SendAsync(mimeMessage, cancellationToken);

            // Disconnect
            await client.DisconnectAsync(true, cancellationToken);

            logger.LogInformation(
                "Email sent successfully to {To} with subject '{Subject}'",
                message.To,
                message.Subject);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to send email to {To} with subject '{Subject}'",
                message.To,
                message.Subject);

            return false;
        }
    }

    public async Task<int> SendBatchEmailAsync(
        IEnumerable<EmailMessage> messages,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            logger.LogWarning("Email service is disabled. Batch emails not sent.");
            return 0;
        }

        var successCount = 0;
        var messageList = messages.ToList();

        try
        {
            using var client = new SmtpClient();

            // Connect once for all emails (more efficient)
            await client.ConnectAsync(
                _settings.SmtpHost,
                _settings.SmtpPort,
                _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
                cancellationToken);

            await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);

            foreach (var message in messageList)
            {
                try
                {
                    var mimeMessage = CreateMimeMessage(message);
                    await client.SendAsync(mimeMessage, cancellationToken);
                    successCount++;

                    logger.LogDebug("Batch email sent to {To}", message.To);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send batch email to {To}", message.To);
                }
            }

            await client.DisconnectAsync(true, cancellationToken);

            logger.LogInformation(
                "Batch email completed: {Sent}/{Total} emails sent successfully",
                successCount,
                messageList.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send batch emails");
        }

        return successCount;
    }

    /// <summary>
    /// Creates a MimeMessage from EmailMessage
    /// </summary>
    private MimeMessage CreateMimeMessage(EmailMessage message)
    {
        var mimeMessage = new MimeMessage();

        // From
        mimeMessage.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));

        // To
        mimeMessage.To.Add(MailboxAddress.Parse(message.To));

        // CC
        if (message.Cc?.Any() == true)
        {
            foreach (var cc in message.Cc)
            {
                mimeMessage.Cc.Add(MailboxAddress.Parse(cc));
            }
        }

        // BCC
        if (message.Bcc?.Any() == true)
        {
            foreach (var bcc in message.Bcc)
            {
                mimeMessage.Bcc.Add(MailboxAddress.Parse(bcc));
            }
        }

        // Reply-To
        if (!string.IsNullOrEmpty(message.ReplyTo))
        {
            mimeMessage.ReplyTo.Add(MailboxAddress.Parse(message.ReplyTo));
        }

        // Subject
        mimeMessage.Subject = message.Subject;

        // Priority
        mimeMessage.Priority = message.Priority switch
        {
            EmailPriority.Urgent => MessagePriority.Urgent,
            EmailPriority.High => MessagePriority.NonUrgent,
            EmailPriority.Low => MessagePriority.NonUrgent,
            _ => MessagePriority.Normal
        };

        // Body
        var bodyBuilder = new BodyBuilder();

        if (message.IsHtml)
        {
            bodyBuilder.HtmlBody = message.Body;
        }
        else
        {
            bodyBuilder.TextBody = message.Body;
        }

        // Attachments
        if (message.Attachments?.Any() == true)
        {
            foreach (var attachment in message.Attachments)
            {
                bodyBuilder.Attachments.Add(
                    attachment.FileName,
                    attachment.Content,
                    ContentType.Parse(attachment.ContentType ?? "application/octet-stream"));
            }
        }

        mimeMessage.Body = bodyBuilder.ToMessageBody();

        return mimeMessage;
    }
}

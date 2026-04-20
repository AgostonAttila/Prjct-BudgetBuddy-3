using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace BudgetBuddy.Shared.Infrastructure.Notification.Email;

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

            await client.ConnectAsync(
                _settings.SmtpHost,
                _settings.SmtpPort,
                _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
                cancellationToken);

            await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);
            await client.SendAsync(mimeMessage, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            logger.LogInformation(
                "Email sent successfully to {To} with subject '{Subject}'",
                message.To, message.Subject);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to send email to {To} with subject '{Subject}'",
                message.To, message.Subject);

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
                successCount, messageList.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send batch emails");
        }

        return successCount;
    }

    private MimeMessage CreateMimeMessage(EmailMessage message)
    {
        var mimeMessage = new MimeMessage();

        mimeMessage.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        mimeMessage.To.Add(MailboxAddress.Parse(message.To));

        if (message.Cc?.Any() == true)
        {
            foreach (var cc in message.Cc)
                mimeMessage.Cc.Add(MailboxAddress.Parse(cc));
        }

        if (message.Bcc?.Any() == true)
        {
            foreach (var bcc in message.Bcc)
                mimeMessage.Bcc.Add(MailboxAddress.Parse(bcc));
        }

        if (!string.IsNullOrEmpty(message.ReplyTo))
            mimeMessage.ReplyTo.Add(MailboxAddress.Parse(message.ReplyTo));

        mimeMessage.Subject = message.Subject;

        mimeMessage.Priority = message.Priority switch
        {
            EmailPriority.Urgent => MessagePriority.Urgent,
            EmailPriority.High => MessagePriority.NonUrgent,
            EmailPriority.Low => MessagePriority.NonUrgent,
            _ => MessagePriority.Normal
        };

        var bodyBuilder = new BodyBuilder();

        if (message.IsHtml)
            bodyBuilder.HtmlBody = message.Body;
        else
            bodyBuilder.TextBody = message.Body;

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

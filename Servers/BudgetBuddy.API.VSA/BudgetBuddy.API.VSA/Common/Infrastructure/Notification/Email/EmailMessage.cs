namespace BudgetBuddy.API.VSA.Common.Infrastructure.Notification.Email;

/// <summary>
/// Represents an email message
/// </summary>
public class EmailMessage
{
    /// <summary>
    /// Recipient email address (required)
    /// </summary>
    public required string To { get; init; }

    /// <summary>
    /// Email subject (required)
    /// </summary>
    public required string Subject { get; init; }

    /// <summary>
    /// Email body content (required)
    /// </summary>
    public required string Body { get; init; }

    /// <summary>
    /// Whether the body is HTML (default: true)
    /// </summary>
    public bool IsHtml { get; init; } = true;

    /// <summary>
    /// Optional CC recipients
    /// </summary>
    public List<string>? Cc { get; init; }

    /// <summary>
    /// Optional BCC recipients
    /// </summary>
    public List<string>? Bcc { get; init; }

    /// <summary>
    /// Optional reply-to address
    /// </summary>
    public string? ReplyTo { get; init; }

    /// <summary>
    /// Optional attachments (file paths)
    /// </summary>
    public List<EmailAttachment>? Attachments { get; init; }

    /// <summary>
    /// Optional priority (default: Normal)
    /// </summary>
    public EmailPriority Priority { get; init; } = EmailPriority.Normal;
}

/// <summary>
/// Email attachment
/// </summary>
public class EmailAttachment
{
    public required string FileName { get; init; }
    public required byte[] Content { get; init; }
    public string? ContentType { get; init; }
}

/// <summary>
/// Email priority levels
/// </summary>
public enum EmailPriority
{
    Low,
    Normal,
    High,
    Urgent
}

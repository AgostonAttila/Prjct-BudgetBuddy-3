namespace BudgetBuddy.Shared.Infrastructure.Notification.Email;

/// <summary>
/// Represents an email message
/// </summary>
public class EmailMessage
{
    public required string To { get; init; }
    public required string Subject { get; init; }
    public required string Body { get; init; }
    public bool IsHtml { get; init; } = true;
    public List<string>? Cc { get; init; }
    public List<string>? Bcc { get; init; }
    public string? ReplyTo { get; init; }
    public List<EmailAttachment>? Attachments { get; init; }
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

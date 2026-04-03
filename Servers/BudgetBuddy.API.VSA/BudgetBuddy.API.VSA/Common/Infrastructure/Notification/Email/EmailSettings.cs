namespace BudgetBuddy.API.VSA.Common.Infrastructure.Notification.Email;

/// <summary>
/// Email service configuration
/// </summary>
public class EmailSettings
{
    public const string SectionName = "Email";

    /// <summary>
    /// SMTP server host (e.g., smtp.gmail.com)
    /// </summary>
    public required string SmtpHost { get; init; }

    /// <summary>
    /// SMTP server port (e.g., 587 for TLS)
    /// </summary>
    public required int SmtpPort { get; init; }

    /// <summary>
    /// SMTP username (usually email address)
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// SMTP password or app password
    /// </summary>
    public required string Password { get; init; }

    /// <summary>
    /// From email address
    /// </summary>
    public required string FromEmail { get; init; }

    /// <summary>
    /// From display name
    /// </summary>
    public required string FromName { get; init; }

    /// <summary>
    /// Enable SSL/TLS (default: true)
    /// </summary>
    public bool EnableSsl { get; init; } = true;

    /// <summary>
    /// SMTP connection timeout in seconds (default: 30)
    /// </summary>
    public int TimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Enable email sending (can disable in development)
    /// </summary>
    public bool Enabled { get; init; } = true;
}

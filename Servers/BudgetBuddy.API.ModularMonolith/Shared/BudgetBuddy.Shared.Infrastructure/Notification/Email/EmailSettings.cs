namespace BudgetBuddy.Shared.Infrastructure.Notification.Email;

/// <summary>
/// Email service configuration
/// </summary>
public class EmailSettings
{
    public const string SectionName = "Email";

    public required string SmtpHost { get; init; }
    public required int SmtpPort { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }
    public required string FromEmail { get; init; }
    public required string FromName { get; init; }
    public bool EnableSsl { get; init; } = true;
    public int TimeoutSeconds { get; init; } = 30;
    public bool Enabled { get; init; } = true;
}

namespace BudgetBuddy.Application.Common.Contracts;

/// <summary>
/// Specialized service for sending authentication-related emails
/// </summary>
public interface IAuthenticationEmailService
{
    Task SendWelcomeEmailAsync(string email, string userName, CancellationToken cancellationToken = default);
    Task SendTwoFactorEnabledEmailAsync(string email, string userName, CancellationToken cancellationToken = default);
    Task SendTwoFactorDisabledEmailAsync(string email, string userName, CancellationToken cancellationToken = default);
    Task SendPasswordChangedEmailAsync(string email, string userName, CancellationToken cancellationToken = default);
    Task SendLoginAlertEmailAsync(string email, string userName, string ipAddress, string userAgent, CancellationToken cancellationToken = default);
}

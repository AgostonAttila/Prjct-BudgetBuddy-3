using BudgetBuddy.Application.Common.Contracts;

namespace BudgetBuddy.Infrastructure.Notification.Email;

public class AuthenticationEmailService(
    IEmailService emailService,
    ILogger<AuthenticationEmailService> logger)
    : IAuthenticationEmailService
{
    public async Task SendWelcomeEmailAsync(string email, string userName, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = EmailTemplates.CreateWelcomeEmail(email, userName);
            var sent = await emailService.SendEmailAsync(message, cancellationToken);

            if (sent)
            {
                logger.LogInformation("Welcome email sent to {Email}", email);
            }
            else
            {
                logger.LogWarning("Failed to send welcome email to {Email}", email);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending welcome email to {Email}", email);
        }
    }

    public async Task SendTwoFactorEnabledEmailAsync(string email, string userName, CancellationToken cancellationToken = default)
    {
        try
        {
            var body = @"
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background-color: #4CAF50; color: white; padding: 20px; text-align: center; }
        .content { padding: 20px; background-color: #f9f9f9; }
        .success { background-color: #d4edda; border-left: 4px solid #28a745; padding: 10px; margin: 10px 0; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🔒 Two-Factor Authentication Enabled</h1>
        </div>
        <div class=""content"">
            <h2>Hi " + userName + @",</h2>
            <div class=""success"">
                <strong>✅ Security Enhanced!</strong>
                <p>Two-factor authentication has been successfully enabled on your BudgetBuddy account.</p>
            </div>
            <p>Your account is now more secure. You'll need your authenticator app to sign in.</p>
            <p><strong>What this means:</strong></p>
            <ul>
                <li>You'll need both your password and a 6-digit code from your authenticator app</li>
                <li>Your account is protected even if someone knows your password</li>
                <li>You can use recovery codes if you lose access to your authenticator</li>
            </ul>
            <p>If you didn't enable 2FA, please contact support immediately.</p>
            <p>Best regards,<br>The BudgetBuddy Team</p>
        </div>
    </div>
</body>
</html>";

            var message = new EmailMessage
            {
                To = email,
                Subject = "Two-Factor Authentication Enabled",
                Body = body,
                IsHtml = true,
                Priority = EmailPriority.High
            };

            await emailService.SendEmailAsync(message, cancellationToken);
            logger.LogInformation("2FA enabled notification sent to {Email}", email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending 2FA enabled email to {Email}", email);
        }
    }

    public async Task SendTwoFactorDisabledEmailAsync(string email, string userName, CancellationToken cancellationToken = default)
    {
        try
        {
            var body = @"
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background-color: #ff9800; color: white; padding: 20px; text-align: center; }
        .content { padding: 20px; background-color: #f9f9f9; }
        .warning { background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 10px; margin: 10px 0; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>⚠️ Two-Factor Authentication Disabled</h1>
        </div>
        <div class=""content"">
            <h2>Hi " + userName + @",</h2>
            <div class=""warning"">
                <strong>⚠️ Security Notice</strong>
                <p>Two-factor authentication has been disabled on your BudgetBuddy account.</p>
            </div>
            <p>Your account security has been reduced. We strongly recommend keeping 2FA enabled.</p>
            <p>If you didn't disable 2FA, please:</p>
            <ul>
                <li>Change your password immediately</li>
                <li>Re-enable two-factor authentication</li>
                <li>Contact support if you suspect unauthorized access</li>
            </ul>
            <p>Best regards,<br>The BudgetBuddy Team</p>
        </div>
    </div>
</body>
</html>";

            var message = new EmailMessage
            {
                To = email,
                Subject = "Two-Factor Authentication Disabled - Security Alert",
                Body = body,
                IsHtml = true,
                Priority = EmailPriority.High
            };

            await emailService.SendEmailAsync(message, cancellationToken);
            logger.LogInformation("2FA disabled notification sent to {Email}", email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending 2FA disabled email to {Email}", email);
        }
    }

    public async Task SendPasswordChangedEmailAsync(string email, string userName, CancellationToken cancellationToken = default)
    {
        try
        {
            var body = @"
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background-color: #2196F3; color: white; padding: 20px; text-align: center; }
        .content { padding: 20px; background-color: #f9f9f9; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🔑 Password Changed</h1>
        </div>
        <div class=""content"">
            <h2>Hi " + userName + @",</h2>
            <p>Your BudgetBuddy password was successfully changed.</p>
            <p>If you didn't make this change, please contact support immediately.</p>
            <p>Best regards,<br>The BudgetBuddy Team</p>
        </div>
    </div>
</body>
</html>";

            var message = new EmailMessage
            {
                To = email,
                Subject = "Password Changed Successfully",
                Body = body,
                IsHtml = true,
                Priority = EmailPriority.High
            };

            await emailService.SendEmailAsync(message, cancellationToken);
            logger.LogInformation("Password changed notification sent to {Email}", email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending password changed email to {Email}", email);
        }
    }

    public async Task SendLoginAlertEmailAsync(string email, string userName, string ipAddress, string userAgent, CancellationToken cancellationToken = default)
    {
        try
        {
            var body = @"
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background-color: #673AB7; color: white; padding: 20px; text-align: center; }
        .content { padding: 20px; background-color: #f9f9f9; }
        .info-box { background-color: #e3f2fd; padding: 10px; margin: 10px 0; border-radius: 5px; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🔐 New Login Detected</h1>
        </div>
        <div class=""content"">
            <h2>Hi " + userName + @",</h2>
            <p>A new login to your BudgetBuddy account was detected.</p>
            <div class=""info-box"">
                <strong>Login Details:</strong><br>
                <strong>IP Address:</strong> " + ipAddress + @"<br>
                <strong>Device:</strong> " + userAgent + @"<br>
                <strong>Time:</strong> " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + @" UTC
            </div>
            <p>If this was you, no action is needed.</p>
            <p>If you don't recognize this login, please:</p>
            <ul>
                <li>Change your password immediately</li>
                <li>Enable two-factor authentication</li>
                <li>Contact support</li>
            </ul>
            <p>Best regards,<br>The BudgetBuddy Team</p>
        </div>
    </div>
</body>
</html>";

            var message = new EmailMessage
            {
                To = email,
                Subject = "New Login to Your Account",
                Body = body,
                IsHtml = true,
                Priority = EmailPriority.Normal
            };

            await emailService.SendEmailAsync(message, cancellationToken);
            logger.LogInformation("Login alert sent to {Email}", email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending login alert email to {Email}", email);
        }
    }
}

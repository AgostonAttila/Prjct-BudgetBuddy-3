
namespace BudgetBuddy.Infrastructure.Notification.Email;

/// <summary>
/// Email templates for common scenarios
/// </summary>
public static class EmailTemplates
{
    /// <summary>
    /// Welcome email for new users
    /// </summary>
    public static EmailMessage CreateWelcomeEmail(string userEmail, string userName)
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
        .button { display: inline-block; padding: 10px 20px; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 5px; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Welcome to BudgetBuddy!</h1>
        </div>
        <div class=""content"">
            <h2>Hi " + userName + @"!</h2>
            <p>Thank you for joining BudgetBuddy. We're excited to help you manage your finances better.</p>
            <p>Here's what you can do:</p>
            <ul>
                <li>Track your expenses and income</li>
                <li>Set budgets and financial goals</li>
                <li>Generate detailed reports</li>
                <li>Monitor your investments</li>
            </ul>
            <p style=""text-align: center;"">
                <a href=""https://budgetbuddy.com/dashboard"" class=""button"">Get Started</a>
            </p>
            <p>If you have any questions, feel free to reach out to our support team.</p>
            <p>Best regards,<br>The BudgetBuddy Team</p>
        </div>
    </div>
</body>
</html>";

        return new EmailMessage
        {
            To = userEmail,
            Subject = "Welcome to BudgetBuddy! 🎉",
            Body = body,
            IsHtml = true
        };
    }

    /// <summary>
    /// Password reset email
    /// </summary>
    public static EmailMessage CreatePasswordResetEmail(string userEmail, string userName, string resetLink)
    {
        var body = @"
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background-color: #FF5722; color: white; padding: 20px; text-align: center; }
        .content { padding: 20px; background-color: #f9f9f9; }
        .button { display: inline-block; padding: 10px 20px; background-color: #FF5722; color: white; text-decoration: none; border-radius: 5px; }
        .warning { background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 10px; margin: 10px 0; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Password Reset Request</h1>
        </div>
        <div class=""content"">
            <h2>Hi " + userName + @",</h2>
            <p>We received a request to reset your BudgetBuddy password.</p>
            <p style=""text-align: center;"">
                <a href=""" + resetLink + @""" class=""button"">Reset Password</a>
            </p>
            <div class=""warning"">
                <strong>⚠️ Security Notice:</strong>
                <ul>
                    <li>This link will expire in 1 hour</li>
                    <li>If you didn't request this, please ignore this email</li>
                    <li>Never share this link with anyone</li>
                </ul>
            </div>
            <p>Best regards,<br>The BudgetBuddy Team</p>
        </div>
    </div>
</body>
</html>";

        return new EmailMessage
        {
            To = userEmail,
            Subject = "Reset Your BudgetBuddy Password",
            Body = body,
            IsHtml = true,
            Priority = EmailPriority.High
        };
    }

    /// <summary>
    /// Budget alert notification
    /// </summary>
    public static EmailMessage CreateBudgetAlertEmail(string userEmail, string userName, string budgetName, decimal spent, decimal limit)
    {
        var percentage = (spent / limit) * 100;
        var color = percentage >= 100 ? "#f44336" : percentage >= 80 ? "#ff9800" : "#ffc107";
        var remaining = limit - spent;

        var body = @"
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background-color: " + color + @"; color: white; padding: 20px; text-align: center; }
        .content { padding: 20px; background-color: #f9f9f9; }
        .progress-bar { width: 100%; background-color: #ddd; border-radius: 5px; overflow: hidden; }
        .progress-fill { height: 30px; background-color: " + color + @"; width: " + percentage.ToString("F0") + @"%; text-align: center; line-height: 30px; color: white; font-weight: bold; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>⚠️ Budget Alert</h1>
        </div>
        <div class=""content"">
            <h2>Hi " + userName + @",</h2>
            <p>Your budget ""<strong>" + budgetName + @"</strong>"" is at <strong>" + percentage.ToString("F0") + @"%</strong> of its limit.</p>
            <div class=""progress-bar"">
                <div class=""progress-fill"">" + percentage.ToString("F0") + @"%</div>
            </div>
            <p style=""margin-top: 20px;"">
                <strong>Spent:</strong> $" + spent.ToString("N2") + @"<br>
                <strong>Limit:</strong> $" + limit.ToString("N2") + @"<br>
                <strong>Remaining:</strong> $" + remaining.ToString("N2") + @"
            </p>
            <p>Consider reviewing your spending to stay within your budget.</p>
            <p>Best regards,<br>The BudgetBuddy Team</p>
        </div>
    </div>
</body>
</html>";

        return new EmailMessage
        {
            To = userEmail,
            Subject = $"Budget Alert: {budgetName} at {percentage:F0}%",
            Body = body,
            IsHtml = true,
            Priority = percentage >= 100 ? EmailPriority.Urgent : EmailPriority.Normal
        };
    }

    /// <summary>
    /// Simple plain text email
    /// </summary>
    public static EmailMessage CreatePlainTextEmail(string to, string subject, string message)
    {
        return new EmailMessage
        {
            To = to,
            Subject = subject,
            Body = message,
            IsHtml = false
        };
    }
}

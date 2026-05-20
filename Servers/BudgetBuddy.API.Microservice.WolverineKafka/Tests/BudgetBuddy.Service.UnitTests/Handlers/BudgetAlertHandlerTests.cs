using BudgetBuddy.Service.Notifications.Messaging.Handlers;
using BudgetBuddy.Shared.Infrastructure.Notification.Email;
using BudgetBuddy.Shared.Messages.Events.Budgets;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace BudgetBuddy.Service.UnitTests.Handlers;

public class BudgetAlertHandlerTests
{
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly ILogger<BudgetAlertHandler> _logger =
        Substitute.For<ILogger<BudgetAlertHandler>>();

    // ── null UserEmail ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Null_UserEmail_returns_early_without_sending_email()
    {
        var evt = new BudgetAlertTriggeredEvent
        {
            BudgetId         = Guid.NewGuid(),
            UserId           = "user-1",
            BudgetName       = "Groceries",
            Limit            = 50_000m,
            CurrentSpending  = 48_000m,
            ThresholdPercent = 90m,
            UserEmail        = null,
        };

        await BudgetAlertHandler.Handle(evt, _emailService, _logger, CancellationToken.None);

        _emailService.ReceivedCalls().Should().BeEmpty(
            because: "no email can be sent when UserEmail is null");
    }

    // ── valid UserEmail ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Valid_UserEmail_calls_SendEmailAsync_once()
    {
        _emailService.SendEmailAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
                     .Returns(true);

        var evt = new BudgetAlertTriggeredEvent
        {
            BudgetId         = Guid.NewGuid(),
            UserId           = "user-1",
            BudgetName       = "Entertainment",
            Limit            = 20_000m,
            CurrentSpending  = 19_500m,
            ThresholdPercent = 95m,
            UserEmail        = "user@example.com",
        };

        await BudgetAlertHandler.Handle(evt, _emailService, _logger, CancellationToken.None);

        await _emailService.Received(1).SendEmailAsync(
            Arg.Any<EmailMessage>(), CancellationToken.None);
    }

    [Fact]
    public async Task Valid_UserEmail_sends_email_to_correct_recipient()
    {
        _emailService.SendEmailAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
                     .Returns(true);

        var evt = new BudgetAlertTriggeredEvent
        {
            BudgetId         = Guid.NewGuid(),
            UserId           = "user-1",
            BudgetName       = "Transport",
            Limit            = 30_000m,
            CurrentSpending  = 28_000m,
            ThresholdPercent = 90m,
            UserEmail        = "driver@example.com",
        };

        await BudgetAlertHandler.Handle(evt, _emailService, _logger, CancellationToken.None);

        await _emailService.Received(1).SendEmailAsync(
            Arg.Is<EmailMessage>(m => m.To == "driver@example.com"),
            CancellationToken.None);
    }
}

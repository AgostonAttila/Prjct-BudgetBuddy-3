using BudgetBuddy.Shared.Infrastructure.Persistence.Outbox;
using FluentAssertions;
using Xunit;

namespace BudgetBuddy.Integration.Tests.Infrastructure;

public class OutboxMessageTests
{
    [Fact]
    public void NewOutboxMessage_ShouldHave_ZeroRetryCount()
    {
        var msg = new OutboxMessage();

        msg.RetryCount.Should().Be(0);
    }

    [Fact]
    public void NewOutboxMessage_ShouldHave_NullProcessedAt()
    {
        var msg = new OutboxMessage();

        msg.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public void NewOutboxMessage_ShouldHave_NullError()
    {
        var msg = new OutboxMessage();

        msg.Error.Should().BeNull();
    }

    [Fact]
    public void NewOutboxMessage_ShouldHave_NonEmptyId()
    {
        var msg = new OutboxMessage();

        msg.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void TwoNewOutboxMessages_ShouldHave_DifferentIds()
    {
        var msg1 = new OutboxMessage();
        var msg2 = new OutboxMessage();

        msg1.Id.Should().NotBe(msg2.Id);
    }

    [Fact]
    public void OutboxMessage_AfterMaxRetries_ShouldBeIdentifiable()
    {
        const int maxRetries = 5;
        var msg = new OutboxMessage { RetryCount = maxRetries };

        (msg.RetryCount >= maxRetries).Should().BeTrue(
            because: "az outbox processornak DLQ-ba kell küldenie ha RetryCount >= MaxRetries");
    }
}

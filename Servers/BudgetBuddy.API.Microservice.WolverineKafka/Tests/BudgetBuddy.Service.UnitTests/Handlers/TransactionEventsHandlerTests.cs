using BudgetBuddy.Service.Accounts.Messaging.Handlers;
using BudgetBuddy.Service.Accounts.ReadModels;
using BudgetBuddy.Shared.Kernel.Enums;
using BudgetBuddy.Shared.Messages.Events.Transactions;
using FluentAssertions;
using NodaTime;
using NSubstitute;
using Xunit;

namespace BudgetBuddy.Service.UnitTests.Handlers;

/// <summary>
/// Tests for TransactionEventsHandler in the Accounts service.
/// Verifies that AccountTransactionTotals deltas are correctly calculated
/// and passed to ApplyDeltaAsync (which handles idempotency internally via LastProcessedMessageId).
/// </summary>
public class TransactionEventsHandlerTests
{
    private readonly IAccountTransactionTotalsRepository _repo =
        Substitute.For<IAccountTransactionTotalsRepository>();

    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid MessageId = Guid.NewGuid();
    private static readonly LocalDate Date  = new(2025, 5, 1);

    // ── TransactionCreatedEvent ──────────────────────────────────────────────────

    [Fact]
    public async Task Created_income_applies_positive_income_delta()
    {
        var evt = new TransactionCreatedEvent
        {
            MessageId       = MessageId,
            AccountId       = AccountId,
            Amount          = 500m,
            TransactionType = TransactionType.Income,
            TransactionDate = Date,
            CurrencyCode    = "HUF",
            UserId          = "u1",
            TransactionId   = Guid.NewGuid(),
        };

        await TransactionEventsHandler.Handle(evt, _repo, CancellationToken.None);

        await _repo.Received(1).ApplyDeltaAsync(
            AccountId, incomeDelta: 500m, expenseDelta: 0m, countDelta: 1, MessageId, CancellationToken.None);
    }

    [Fact]
    public async Task Created_expense_applies_positive_expense_delta()
    {
        var evt = new TransactionCreatedEvent
        {
            MessageId       = MessageId,
            AccountId       = AccountId,
            Amount          = 300m,
            TransactionType = TransactionType.Expense,
            TransactionDate = Date,
            CurrencyCode    = "HUF",
            UserId          = "u1",
            TransactionId   = Guid.NewGuid(),
        };

        await TransactionEventsHandler.Handle(evt, _repo, CancellationToken.None);

        await _repo.Received(1).ApplyDeltaAsync(
            AccountId, incomeDelta: 0m, expenseDelta: 300m, countDelta: 1, MessageId, CancellationToken.None);
    }

    // ── TransactionUpdatedEvent ──────────────────────────────────────────────────

    [Fact]
    public async Task Updated_income_amount_increased_applies_positive_income_delta()
    {
        var evt = new TransactionUpdatedEvent
        {
            MessageId       = MessageId,
            AccountId       = AccountId,
            Amount          = 600m,
            OldAmount       = 400m,
            TransactionType = TransactionType.Income,
            TransactionDate = Date,
            CurrencyCode    = "HUF",
            UserId          = "u1",
            TransactionId   = Guid.NewGuid(),
        };

        await TransactionEventsHandler.Handle(evt, _repo, CancellationToken.None);

        await _repo.Received(1).ApplyDeltaAsync(
            AccountId, incomeDelta: 200m, expenseDelta: 0m, countDelta: 0, MessageId, CancellationToken.None);
    }

    [Fact]
    public async Task Updated_expense_amount_decreased_applies_negative_expense_delta()
    {
        var evt = new TransactionUpdatedEvent
        {
            MessageId       = MessageId,
            AccountId       = AccountId,
            Amount          = 100m,
            OldAmount       = 300m,
            TransactionType = TransactionType.Expense,
            TransactionDate = Date,
            CurrencyCode    = "HUF",
            UserId          = "u1",
            TransactionId   = Guid.NewGuid(),
        };

        await TransactionEventsHandler.Handle(evt, _repo, CancellationToken.None);

        await _repo.Received(1).ApplyDeltaAsync(
            AccountId, incomeDelta: 0m, expenseDelta: -200m, countDelta: 0, MessageId, CancellationToken.None);
    }

    [Fact]
    public async Task Updated_amount_unchanged_does_not_call_repository()
    {
        var evt = new TransactionUpdatedEvent
        {
            MessageId       = MessageId,
            AccountId       = AccountId,
            Amount          = 500m,
            OldAmount       = 500m,
            TransactionType = TransactionType.Expense,
            TransactionDate = Date,
            CurrencyCode    = "HUF",
            UserId          = "u1",
            TransactionId   = Guid.NewGuid(),
        };

        await TransactionEventsHandler.Handle(evt, _repo, CancellationToken.None);

        await _repo.DidNotReceiveWithAnyArgs().ApplyDeltaAsync(
            Guid.Empty, default, default, default, Guid.Empty, default);
    }

    // ── TransactionDeletedEvent ──────────────────────────────────────────────────

    [Fact]
    public async Task Deleted_income_applies_negative_income_delta_and_minus_one_count()
    {
        var evt = new TransactionDeletedEvent
        {
            MessageId       = MessageId,
            AccountId       = AccountId,
            Amount          = 800m,
            TransactionType = TransactionType.Income,
            TransactionDate = Date,
            CurrencyCode    = "HUF",
            UserId          = "u1",
            TransactionId   = Guid.NewGuid(),
        };

        await TransactionEventsHandler.Handle(evt, _repo, CancellationToken.None);

        await _repo.Received(1).ApplyDeltaAsync(
            AccountId, incomeDelta: -800m, expenseDelta: 0m, countDelta: -1, MessageId, CancellationToken.None);
    }

    [Fact]
    public async Task Deleted_expense_applies_negative_expense_delta_and_minus_one_count()
    {
        var evt = new TransactionDeletedEvent
        {
            MessageId       = MessageId,
            AccountId       = AccountId,
            Amount          = 250m,
            TransactionType = TransactionType.Expense,
            TransactionDate = Date,
            CurrencyCode    = "HUF",
            UserId          = "u1",
            TransactionId   = Guid.NewGuid(),
        };

        await TransactionEventsHandler.Handle(evt, _repo, CancellationToken.None);

        await _repo.Received(1).ApplyDeltaAsync(
            AccountId, incomeDelta: 0m, expenseDelta: -250m, countDelta: -1, MessageId, CancellationToken.None);
    }
}

using BudgetBuddy.Service.Transactions.Domain;
using BudgetBuddy.Service.Transactions.Messaging.Handlers;
using BudgetBuddy.Service.Transactions.Persistence;
using BudgetBuddy.Service.Transactions.Sagas;
using BudgetBuddy.Shared.Infrastructure.Security.Encryption;
using BudgetBuddy.Shared.Kernel.Enums;
using BudgetBuddy.Shared.Messages.Events.Accounts;
using BudgetBuddy.Shared.Messages.Events.Transactions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NodaTime;
using Wolverine;
using Xunit;

namespace BudgetBuddy.Service.UnitTests.Handlers;

public class AccountBalanceSagaHandlerTests : IDisposable
{
    private readonly TransactionsDbContext _db;
    private readonly IMessageContext       _bus    = Substitute.For<IMessageContext>();
    private readonly ILogger<AccountBalanceSagaHandler> _logger =
        Substitute.For<ILogger<AccountBalanceSagaHandler>>();

    public AccountBalanceSagaHandlerTests()
    {
        var enc = Substitute.For<IEncryptionService>();
        enc.Encrypt(Arg.Any<string?>(), Arg.Any<string>()).Returns(x => x.ArgAt<string?>(0));
        enc.Decrypt(Arg.Any<string?>(), Arg.Any<string>()).Returns(x => x.ArgAt<string?>(0));

        var opts = new DbContextOptionsBuilder<TransactionsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new TransactionsDbContext(opts, enc);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _db.Dispose();
        }
    }

    // ── helpers ─────────────────────────────────────────────────────────────────

    private async Task<Transaction> SeedTransactionAsync(TransactionSagaStep step = TransactionSagaStep.Started)
    {
        var t = new Transaction
        {
            Id              = Guid.NewGuid(),
            AccountId       = Guid.NewGuid(),
            Amount          = 1_000m,
            CurrencyCode    = "HUF",
            TransactionType = TransactionType.Expense,
            PaymentType     = PaymentType.Card,
            TransactionDate = new LocalDate(2025, 5, 1),
            UserId          = "user-1",
            SagaStep        = step,
        };
        _db.Transactions.Add(t);
        await _db.SaveChangesAsync();
        return t;
    }

    private AccountBalanceReservedEvent MakeEvent(Guid transactionId, bool success, string? reason = null) =>
        new()
        {
            TransactionId = transactionId,
            Success       = success,
            Reason        = reason,
        };

    // ── tests ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Transaction_not_found_throws_InvalidOperationException()
    {
        var evt = MakeEvent(Guid.NewGuid(), success: false);

        var act = () => AccountBalanceSagaHandler.Handle(evt, _db, _bus, _logger, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{evt.TransactionId}*");
    }

    [Fact]
    public async Task Success_true_sets_SagaStep_Confirmed()
    {
        var tx  = await SeedTransactionAsync();
        var evt = MakeEvent(tx.Id, success: true);

        await AccountBalanceSagaHandler.Handle(evt, _db, _bus, _logger, CancellationToken.None);

        var updated = await _db.Transactions.FindAsync(tx.Id);
        updated!.SagaStep.Should().Be(TransactionSagaStep.Confirmed);
    }

    [Fact]
    public async Task Success_true_does_not_publish_any_message()
    {
        var tx  = await SeedTransactionAsync();
        var evt = MakeEvent(tx.Id, success: true);

        await AccountBalanceSagaHandler.Handle(evt, _db, _bus, _logger, CancellationToken.None);

        _bus.ReceivedCalls().Should().BeEmpty(because: "balance confirmation should not publish any message");
    }

    [Fact]
    public async Task Success_false_sets_original_SagaStep_Compensated()
    {
        var tx  = await SeedTransactionAsync();
        var evt = MakeEvent(tx.Id, success: false, reason: "Insufficient funds");

        await AccountBalanceSagaHandler.Handle(evt, _db, _bus, _logger, CancellationToken.None);

        var updated = await _db.Transactions.FindAsync(tx.Id);
        updated!.SagaStep.Should().Be(TransactionSagaStep.Compensated);
    }

    [Fact]
    public async Task Success_false_creates_reversal_transaction()
    {
        var tx  = await SeedTransactionAsync();
        var evt = MakeEvent(tx.Id, success: false, reason: "Insufficient funds");

        await AccountBalanceSagaHandler.Handle(evt, _db, _bus, _logger, CancellationToken.None);

        // Wolverine commits automatically after handler — inspect the change tracker
        // to verify the reversal was added without needing SaveChangesAsync in the test.
        var reversal = _db.ChangeTracker.Entries<Transaction>()
            .Where(e => e.Entity.Id != tx.Id)
            .Select(e => e.Entity)
            .SingleOrDefault();

        reversal.Should().NotBeNull();
        reversal!.Amount.Should().Be(-tx.Amount);
        reversal.AccountId.Should().Be(tx.AccountId);
        reversal.UserId.Should().Be(tx.UserId);
        reversal.IsHidden.Should().BeTrue();
        reversal.SagaStep.Should().Be(TransactionSagaStep.Confirmed);
        reversal.Note.Should().Contain($"Reversal of {tx.Id}");
        reversal.Note.Should().Contain("Insufficient funds");
    }

    [Fact]
    public async Task Success_false_publishes_TransactionReversedEvent()
    {
        var tx  = await SeedTransactionAsync();
        var evt = MakeEvent(tx.Id, success: false, reason: "Insufficient funds");

        await AccountBalanceSagaHandler.Handle(evt, _db, _bus, _logger, CancellationToken.None);

        await _bus.Received(1).PublishAsync(Arg.Is<TransactionReversedEvent>(e =>
            e.OriginalTransactionId == tx.Id &&
            e.Reason == "Insufficient funds"));
    }

    [Fact]
    public async Task Idempotency_skips_when_SagaStep_is_Compensating()
    {
        var tx  = await SeedTransactionAsync(TransactionSagaStep.Compensating);
        var evt = MakeEvent(tx.Id, success: false);

        await AccountBalanceSagaHandler.Handle(evt, _db, _bus, _logger, CancellationToken.None);

        var trackedCount = _db.ChangeTracker.Entries<Transaction>().Count();
        trackedCount.Should().Be(1, because: "no new reversal should be added to the context");
        _bus.ReceivedCalls().Should().BeEmpty(because: "idempotent skip must not publish");
    }

    [Fact]
    public async Task Idempotency_skips_when_SagaStep_is_Compensated()
    {
        var tx  = await SeedTransactionAsync(TransactionSagaStep.Compensated);
        var evt = MakeEvent(tx.Id, success: false);

        await AccountBalanceSagaHandler.Handle(evt, _db, _bus, _logger, CancellationToken.None);

        var trackedCount = _db.ChangeTracker.Entries<Transaction>().Count();
        trackedCount.Should().Be(1, because: "no new reversal should be added to the context");
        _bus.ReceivedCalls().Should().BeEmpty(because: "idempotent skip must not publish");
    }

    [Fact]
    public async Task Reversal_inherits_currency_and_transaction_type_from_original()
    {
        var tx  = await SeedTransactionAsync();
        var evt = MakeEvent(tx.Id, success: false);

        await AccountBalanceSagaHandler.Handle(evt, _db, _bus, _logger, CancellationToken.None);

        var reversal = _db.ChangeTracker.Entries<Transaction>()
            .Where(e => e.Entity.Id != tx.Id)
            .Select(e => e.Entity)
            .Single();

        reversal.CurrencyCode.Should().Be(tx.CurrencyCode);
        reversal.TransactionType.Should().Be(tx.TransactionType);
        reversal.PaymentType.Should().Be(tx.PaymentType);
        reversal.TransactionDate.Should().Be(tx.TransactionDate);
        reversal.IsTransfer.Should().BeFalse();
    }

    [Fact]
    public async Task Null_reason_falls_back_to_default_string_in_published_event()
    {
        var tx  = await SeedTransactionAsync();
        var evt = MakeEvent(tx.Id, success: false, reason: null);

        await AccountBalanceSagaHandler.Handle(evt, _db, _bus, _logger, CancellationToken.None);

        await _bus.Received(1).PublishAsync(Arg.Is<TransactionReversedEvent>(e =>
            e.Reason == "Saga compensation"));
    }
}

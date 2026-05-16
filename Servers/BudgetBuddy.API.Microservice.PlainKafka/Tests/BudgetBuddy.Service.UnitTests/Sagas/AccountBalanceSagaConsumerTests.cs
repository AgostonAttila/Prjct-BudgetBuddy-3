using System.Reflection;
using BudgetBuddy.Service.Transactions.Domain;
using BudgetBuddy.Service.Transactions.Messaging.Consumers;
using BudgetBuddy.Service.Transactions.Persistence;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Infrastructure.Persistence.Outbox;
using BudgetBuddy.Shared.Infrastructure.Security.Encryption;
using BudgetBuddy.Shared.Kernel.Enums;
using BudgetBuddy.Shared.Messages.Events.Accounts;
using BudgetBuddy.Shared.Messages.Events.Transactions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using Xunit;

namespace BudgetBuddy.Service.UnitTests.Sagas;

/// <summary>
/// Unit tests for AccountBalanceSagaConsumer compensation logic.
/// Uses InMemory database — the OutboxInterceptor is intentionally NOT registered
/// so we test saga business logic in isolation.
/// ProcessAsync is invoked via reflection since AccountBalanceSagaConsumer is sealed.
/// </summary>
public sealed class AccountBalanceSagaConsumerTests : IDisposable
{
    private readonly TransactionsDbContext _db;
    private readonly DomainEventCollector _collector;
    private readonly AccountBalanceSagaConsumer _saga;

    private static readonly MethodInfo ProcessAsyncMethod =
        typeof(AccountBalanceSagaConsumer)
            .GetMethod("ProcessAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;

    public AccountBalanceSagaConsumerTests()
    {
        var options = new DbContextOptionsBuilder<TransactionsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db        = new TransactionsDbContext(options, new PassThroughEncryptionService());
        _collector = new DomainEventCollector();

        var sp = BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

        _saga = new AccountBalanceSagaConsumer(
            new KafkaSettings(), scopeFactory,
            NullLogger<AccountBalanceSagaConsumer>.Instance);
    }

    // ── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Success_event_makes_no_db_changes()
    {
        var msg = new AccountBalanceReservedEvent { Success = true, TransactionId = Guid.NewGuid() };

        await InvokeProcessAsync(msg);

        (await _db.Transactions.CountAsync()).Should().Be(0);
        _collector.GetAndClear().Should().BeEmpty();
    }

    [Fact]
    public async Task Failure_when_original_not_found_makes_no_db_changes()
    {
        var msg = new AccountBalanceReservedEvent
        {
            Success       = false,
            TransactionId = Guid.NewGuid(),
            Reason        = "Insufficient funds"
        };

        await InvokeProcessAsync(msg);

        (await _db.Transactions.CountAsync()).Should().Be(0);
        _collector.GetAndClear().Should().BeEmpty();
    }

    [Fact]
    public async Task Failure_creates_reversal_transaction_and_collects_event()
    {
        var original = SeedTransaction(100m);
        var msg = new AccountBalanceReservedEvent
        {
            Success       = false,
            TransactionId = original.Id,
            CorrelationId = Guid.NewGuid(),
            Reason        = "Insufficient funds"
        };

        await InvokeProcessAsync(msg);

        (await _db.Transactions.CountAsync()).Should().Be(2);

        var reversal = await _db.Transactions.SingleAsync(t => t.Id != original.Id);
        reversal.Amount.Should().Be(-original.Amount);
        reversal.AccountId.Should().Be(original.AccountId);
        reversal.Note.Should().Contain("[SAGA COMPENSATION]");
        reversal.Note.Should().Contain(original.Id.ToString());
        reversal.Note.Should().Contain("Insufficient funds");

        var events = _collector.GetAndClear();
        events.Should().HaveCount(1);
        var ev = events[0].Should().BeOfType<TransactionReversedEvent>().Subject;
        ev.OriginalTransactionId.Should().Be(original.Id);
        ev.ReversalTransactionId.Should().Be(reversal.Id);
    }

    [Fact]
    public async Task Duplicate_failure_event_does_not_create_second_compensation()
    {
        var original = SeedTransaction(200m);
        var compensationNote = $"[SAGA COMPENSATION] Reversal of {original.Id}";
        SeedCompensation(original, compensationNote);

        var msg = new AccountBalanceReservedEvent
        {
            Success       = false,
            TransactionId = original.Id,
            Reason        = "Insufficient funds"
        };

        await InvokeProcessAsync(msg);

        // Still 2 — original + existing compensation, no new reversal
        (await _db.Transactions.CountAsync()).Should().Be(2);
        _collector.GetAndClear().Should().BeEmpty();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private IServiceProvider BuildServiceProvider() =>
        new ServiceCollection()
            .AddSingleton(_db)
            .AddSingleton<IDomainEventCollector>(_collector)
            .AddSingleton<IClock>(SystemClock.Instance)
            .BuildServiceProvider();

    private async Task InvokeProcessAsync(AccountBalanceReservedEvent msg)
    {
        var sp = BuildServiceProvider();
        var task = (Task)ProcessAsyncMethod.Invoke(_saga, [msg, sp, CancellationToken.None])!;
        await task;
    }

    private Transaction SeedTransaction(decimal amount)
    {
        var tx = new Transaction
        {
            Id              = Guid.NewGuid(),
            AccountId       = Guid.NewGuid(),
            Amount          = amount,
            CurrencyCode    = "HUF",
            TransactionType = TransactionType.Expense,
            PaymentType     = PaymentType.Card,
            TransactionDate = new LocalDate(2024, 1, 15),
            UserId          = "user-1",
        };
        _db.Transactions.Add(tx);
        _db.SaveChanges();
        return tx;
    }

    private void SeedCompensation(Transaction original, string compensationNote)
    {
        var comp = new Transaction
        {
            Id              = Guid.NewGuid(),
            AccountId       = original.AccountId,
            Amount          = -original.Amount,
            CurrencyCode    = original.CurrencyCode,
            TransactionType = original.TransactionType,
            PaymentType     = original.PaymentType,
            TransactionDate = original.TransactionDate,
            Note            = $"{compensationNote}: Insufficient funds",
            UserId          = original.UserId,
        };
        _db.Transactions.Add(comp);
        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }

    // ── Test double ──────────────────────────────────────────────────────────

    /// <summary>Pass-through encryption for InMemory database tests.</summary>
    private sealed class PassThroughEncryptionService : IEncryptionService
    {
        public string? Encrypt(string? plainText, string purpose) => plainText;
        public string? Decrypt(string? encryptedText, string purpose) => encryptedText;
    }
}

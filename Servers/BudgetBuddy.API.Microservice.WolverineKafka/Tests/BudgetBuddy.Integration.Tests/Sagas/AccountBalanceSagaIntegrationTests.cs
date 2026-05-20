using BudgetBuddy.Integration.Tests.Infrastructure;
using BudgetBuddy.Service.Transactions.Domain;
using BudgetBuddy.Service.Transactions.Messaging.Handlers;
using BudgetBuddy.Service.Transactions.Persistence;
using BudgetBuddy.Service.Transactions.Sagas;
using BudgetBuddy.Shared.Kernel.Enums;
using BudgetBuddy.Shared.Messages.Events.Accounts;
using BudgetBuddy.Shared.Messages.Events.Transactions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime;
using NSubstitute;
using Wolverine;
using Xunit;

namespace BudgetBuddy.Integration.Tests.Sagas;

/// <summary>
/// Handler-level integration tests for the balance-reservation choreography saga.
///
/// Pipeline under test:
///   AccountBalanceSagaHandler.Handle(AccountBalanceReservedEvent, TransactionsDbContext, ...)
///
/// Why the handler is called directly instead of via IMessageBus.InvokeAsync:
///   TransactionsApiFactory calls DisableAllWolverineMessagePersistence(), which removes
///   the Wolverine EF Core transaction wrapper (UseEntityFrameworkCoreTransactions).
///   Without the wrapper, Wolverine never calls SaveChangesAsync after the handler,
///   so DB assertions would always see stale state.
///   Calling the static handler directly lets us control SaveChangesAsync explicitly
///   while still exercising the full handler logic against a real PostgreSQL database.
///
/// IMessageContext is substituted with NSubstitute to verify that
/// TransactionReversedEvent is published as part of the compensation flow.
/// </summary>
[Collection(nameof(TransactionsApiCollection))]
public class AccountBalanceSagaIntegrationTests(TransactionsApiFactory factory) : IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();
    public Task DisposeAsync()    => Task.CompletedTask;

    // ── Test 1: compensation path ──────────────────────────────────────────────

    [Fact]
    public async Task FailedReservation_SetsTransactionCompensated_AndCreatesHiddenReversal()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        const decimal amount = 500m;
        await SeedStartedTransactionAsync(transactionId, amount);

        var bus = Substitute.For<IMessageContext>();

        // Act — call handler directly with real DbContext; SaveChanges replaces Wolverine wrapper
        await using var scope = factory.Services.CreateAsyncScope();
        var db     = scope.ServiceProvider.GetRequiredService<TransactionsDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AccountBalanceSagaHandler>>();

        await AccountBalanceSagaHandler.Handle(
            new AccountBalanceReservedEvent { TransactionId = transactionId, Success = false, Reason = "Insufficient balance" },
            db, bus, logger, CancellationToken.None);

        await db.SaveChangesAsync();

        // Assert — original transaction is fully compensated
        await using var assertScope = factory.Services.CreateAsyncScope();
        var assertDb  = assertScope.ServiceProvider.GetRequiredService<TransactionsDbContext>();
        var original  = await assertDb.Transactions.FindAsync(transactionId);

        original!.SagaStep.Should().Be(TransactionSagaStep.Compensated,
            "handler must advance saga to Compensated after creating the reversal");

        // Assert — one hidden reversal with negated amount
        var reversals = await assertDb.Transactions.Where(t => t.IsHidden).ToListAsync();
        reversals.Should().ContainSingle("exactly one compensation reversal must be created");
        reversals[0].Amount.Should().Be(-amount, "reversal negates the original amount");
        reversals[0].AccountId.Should().Be(original.AccountId);
        reversals[0].SagaStep.Should().Be(TransactionSagaStep.Confirmed,
            "the reversal itself requires no further balance reservation");

        // Assert — TransactionReversedEvent was published via IMessageContext
        await bus.Received(1).PublishAsync(
            Arg.Is<TransactionReversedEvent>(e =>
                e.OriginalTransactionId == transactionId &&
                e.ReversalTransactionId == reversals[0].Id));
    }

    // ── Test 2: success path ───────────────────────────────────────────────────

    [Fact]
    public async Task SuccessfulReservation_SetsTransactionConfirmed_AndCreatesNoReversal()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        await SeedStartedTransactionAsync(transactionId, amount: 250m);

        var bus = Substitute.For<IMessageContext>();

        // Act
        await using var scope = factory.Services.CreateAsyncScope();
        var db     = scope.ServiceProvider.GetRequiredService<TransactionsDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AccountBalanceSagaHandler>>();

        await AccountBalanceSagaHandler.Handle(
            new AccountBalanceReservedEvent { TransactionId = transactionId, Success = true },
            db, bus, logger, CancellationToken.None);

        await db.SaveChangesAsync();

        // Assert
        await using var assertScope = factory.Services.CreateAsyncScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<TransactionsDbContext>();

        var original = await assertDb.Transactions.FindAsync(transactionId);
        original!.SagaStep.Should().Be(TransactionSagaStep.Confirmed);

        var hiddenCount = await assertDb.Transactions.CountAsync(t => t.IsHidden);
        hiddenCount.Should().Be(0, "successful reservation must not produce a reversal");

        await bus.DidNotReceive().PublishAsync(Arg.Any<TransactionReversedEvent>());
    }

    // ── Test 3: idempotency ────────────────────────────────────────────────────

    [Fact]
    public async Task DuplicateFailedReservation_IsIdempotent_ProducesExactlyOneReversal()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        await SeedStartedTransactionAsync(transactionId, amount: 100m);

        var evt = new AccountBalanceReservedEvent
        {
            TransactionId = transactionId,
            Success       = false,
            Reason        = "Insufficient balance",
        };

        // Act — deliver the same compensation event twice
        await using var scope1 = factory.Services.CreateAsyncScope();
        var db1     = scope1.ServiceProvider.GetRequiredService<TransactionsDbContext>();
        var logger1 = scope1.ServiceProvider.GetRequiredService<ILogger<AccountBalanceSagaHandler>>();
        await AccountBalanceSagaHandler.Handle(evt, db1, Substitute.For<IMessageContext>(), logger1, CancellationToken.None);
        await db1.SaveChangesAsync();

        await using var scope2 = factory.Services.CreateAsyncScope();
        var db2     = scope2.ServiceProvider.GetRequiredService<TransactionsDbContext>();
        var logger2 = scope2.ServiceProvider.GetRequiredService<ILogger<AccountBalanceSagaHandler>>();
        await AccountBalanceSagaHandler.Handle(evt, db2, Substitute.For<IMessageContext>(), logger2, CancellationToken.None);
        await db2.SaveChangesAsync();

        // Assert — SagaStep state machine prevents a second reversal
        await using var assertScope = factory.Services.CreateAsyncScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<TransactionsDbContext>();

        var hiddenCount = await assertDb.Transactions.CountAsync(t => t.IsHidden);
        hiddenCount.Should().Be(1, "SagaStep idempotency guard must prevent duplicate reversals");
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private async Task SeedStartedTransactionAsync(Guid id, decimal amount)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TransactionsDbContext>();

        db.Transactions.Add(new Transaction
        {
            Id              = id,
            AccountId       = Guid.NewGuid(),
            Amount          = amount,
            CurrencyCode    = "USD",
            TransactionType = TransactionType.Expense,
            PaymentType     = default,
            TransactionDate = new LocalDate(2026, 5, 20),
            IsTransfer      = false,
            UserId          = "saga-integration-test-user",
            SagaStep        = TransactionSagaStep.Started,
        });

        await db.SaveChangesAsync();
    }
}

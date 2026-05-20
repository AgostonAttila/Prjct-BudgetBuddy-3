using BudgetBuddy.Service.Analytics.Messaging.Handlers;
using BudgetBuddy.Service.Analytics.ReadModels;
using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Kernel.Enums;
using BudgetBuddy.Shared.Messages.Events.Transactions;
using FluentAssertions;
using NodaTime;
using NSubstitute;
using Xunit;

namespace BudgetBuddy.Service.UnitTests.Handlers;

public class AnalyticsTransactionEventsHandlerTests
{
    private readonly ITransactionReadModelRepository _repo =
        Substitute.For<ITransactionReadModelRepository>();

    private readonly IUserCacheInvalidator _cache =
        Substitute.For<IUserCacheInvalidator>();

    private static readonly Guid      AccountId     = Guid.NewGuid();
    private static readonly Guid      TransactionId = Guid.NewGuid();
    private static readonly LocalDate Date          = new(2025, 5, 10);

    // ── TransactionCreatedEvent ──────────────────────────────────────────────────

    [Fact]
    public async Task Created_invalidates_cache_and_upserts_read_model()
    {
        var evt = new TransactionCreatedEvent
        {
            TransactionId   = TransactionId,
            UserId          = "user-1",
            AccountId       = AccountId,
            Amount          = 1_000m,
            CurrencyCode    = "HUF",
            TransactionType = TransactionType.Expense,
            TransactionDate = Date,
        };

        await TransactionEventsHandler.Handle(evt, _repo, _cache, CancellationToken.None);

        await _cache.Received(1).InvalidateAsync("user-1", CancellationToken.None);
        await _repo.Received(1).UpsertAsync(
            Arg.Is<TransactionReadModel>(m =>
                m.TransactionId   == TransactionId          &&
                m.UserId          == "user-1"               &&
                m.AccountId       == AccountId              &&
                m.Amount          == 1_000m                 &&
                m.TransactionType == TransactionType.Expense &&
                m.TransactionDate == Date),
            CancellationToken.None);
    }

    [Fact]
    public async Task Created_invalidates_cache_before_upsert()
    {
        // Verify ordering: cache invalidation must happen before upsert
        var callOrder = new List<string>();
        _cache.When(c => c.InvalidateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()))
              .Do(_ => callOrder.Add("cache"));
        _repo.When(r => r.UpsertAsync(Arg.Any<TransactionReadModel>(), Arg.Any<CancellationToken>()))
             .Do(_ => callOrder.Add("repo"));

        var evt = new TransactionCreatedEvent
        {
            TransactionId   = TransactionId,
            UserId          = "user-1",
            AccountId       = AccountId,
            TransactionType = TransactionType.Income,
            TransactionDate = Date,
        };

        await TransactionEventsHandler.Handle(evt, _repo, _cache, CancellationToken.None);

        callOrder.Should().Equal("cache", "repo");
    }

    // ── TransactionUpdatedEvent ──────────────────────────────────────────────────

    [Fact]
    public async Task Updated_invalidates_cache_and_upserts_read_model()
    {
        var evt = new TransactionUpdatedEvent
        {
            TransactionId   = TransactionId,
            UserId          = "user-2",
            AccountId       = AccountId,
            Amount          = 2_000m,
            OldAmount       = 1_500m,
            CurrencyCode    = "EUR",
            TransactionType = TransactionType.Income,
            TransactionDate = Date,
        };

        await TransactionEventsHandler.Handle(evt, _repo, _cache, CancellationToken.None);

        await _cache.Received(1).InvalidateAsync("user-2", CancellationToken.None);
        await _repo.Received(1).UpsertAsync(
            Arg.Is<TransactionReadModel>(m =>
                m.TransactionId == TransactionId &&
                m.Amount        == 2_000m        &&
                m.CurrencyCode  == "EUR"),
            CancellationToken.None);
    }

    // ── TransactionDeletedEvent ──────────────────────────────────────────────────

    [Fact]
    public async Task Deleted_invalidates_cache_and_removes_read_model()
    {
        var evt = new TransactionDeletedEvent
        {
            TransactionId   = TransactionId,
            UserId          = "user-1",
            AccountId       = AccountId,
            Amount          = 500m,
            CurrencyCode    = "HUF",
            TransactionType = TransactionType.Expense,
            TransactionDate = Date,
        };

        await TransactionEventsHandler.Handle(evt, _repo, _cache, CancellationToken.None);

        await _cache.Received(1).InvalidateAsync("user-1", CancellationToken.None);
        await _repo.Received(1).DeleteAsync(TransactionId, CancellationToken.None);
    }

    [Fact]
    public async Task Deleted_does_not_call_UpsertAsync()
    {
        var evt = new TransactionDeletedEvent
        {
            TransactionId   = TransactionId,
            UserId          = "user-1",
            AccountId       = AccountId,
            Amount          = 100m,
            CurrencyCode    = "HUF",
            TransactionType = TransactionType.Expense,
            TransactionDate = Date,
        };

        await TransactionEventsHandler.Handle(evt, _repo, _cache, CancellationToken.None);

        await _repo.DidNotReceiveWithAnyArgs().UpsertAsync(default!, default);
    }
}

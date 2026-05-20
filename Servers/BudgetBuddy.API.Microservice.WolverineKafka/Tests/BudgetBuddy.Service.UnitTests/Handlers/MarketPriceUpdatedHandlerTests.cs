using BudgetBuddy.Service.Analytics.Messaging.Handlers;
using BudgetBuddy.Service.Analytics.Persistence;
using BudgetBuddy.Service.Analytics.ReadModels;
using BudgetBuddy.Shared.Messages.Events.Investments;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NodaTime;
using NSubstitute;
using Xunit;

namespace BudgetBuddy.Service.UnitTests.Handlers;

public class MarketPriceUpdatedHandlerTests : IDisposable
{
    private readonly AnalyticsDbContext _db;
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly ILogger<MarketPriceUpdatedHandler> _logger =
        Substitute.For<ILogger<MarketPriceUpdatedHandler>>();

    private static readonly Instant FixedNow =
        Instant.FromUtc(2025, 5, 10, 12, 0, 0);

    public MarketPriceUpdatedHandlerTests()
    {
        var opts = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AnalyticsDbContext(opts);
        _clock.GetCurrentInstant().Returns(FixedNow);
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

    // ── helpers ──────────────────────────────────────────────────────────────────

    private static MarketPriceUpdatedEvent MakeEvent(string symbol, decimal price, string currency,
        DateTime? occurredAt = null) =>
        new()
        {
            Symbol     = symbol,
            Price      = price,
            Currency   = currency,
            OccurredAt = occurredAt ?? new DateTime(2025, 5, 10, 9, 30, 0, DateTimeKind.Utc),
        };

    // ── tests ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Non_USD_currency_is_skipped_and_no_snapshot_is_added()
    {
        var evt = MakeEvent("BTC", 60_000m, "EUR");

        await MarketPriceUpdatedHandler.Handle(evt, _db, _clock, _logger, CancellationToken.None);

        _db.ChangeTracker.Entries<PriceSnapshotReadModel>().Should().BeEmpty(
            because: "non-USD price events must not be persisted");
    }

    [Fact]
    public async Task Non_USD_currency_case_insensitive_check_skips_eur()
    {
        var evt = MakeEvent("ETH", 3_000m, "eur");

        await MarketPriceUpdatedHandler.Handle(evt, _db, _clock, _logger, CancellationToken.None);

        _db.ChangeTracker.Entries<PriceSnapshotReadModel>().Should().BeEmpty();
    }

    [Fact]
    public async Task New_USD_snapshot_is_added_to_change_tracker()
    {
        var occurredAt = new DateTime(2025, 5, 10, 9, 30, 0, DateTimeKind.Utc);
        var evt        = MakeEvent("AAPL", 175m, "USD", occurredAt);

        await MarketPriceUpdatedHandler.Handle(evt, _db, _clock, _logger, CancellationToken.None);

        var entry = _db.ChangeTracker.Entries<PriceSnapshotReadModel>()
            .Single();
        entry.Entity.Symbol.Should().Be("AAPL");
        entry.Entity.PriceUsd.Should().Be(175m);
        entry.Entity.Date.Should().Be(new LocalDate(2025, 5, 10));
        entry.Entity.SyncedAt.Should().Be(FixedNow);
        entry.State.Should().Be(EntityState.Added);
    }

    [Fact]
    public async Task Existing_snapshot_for_same_symbol_and_date_is_updated_in_place()
    {
        var occurredAt = new DateTime(2025, 5, 10, 9, 30, 0, DateTimeKind.Utc);
        var date       = new LocalDate(2025, 5, 10);

        // Seed an existing snapshot
        var existing = new PriceSnapshotReadModel
        {
            Symbol   = "MSFT",
            Date     = date,
            PriceUsd = 300m,
            SyncedAt = Instant.FromUtc(2025, 5, 10, 8, 0, 0),
        };
        _db.PriceSnapshots.Add(existing);
        await _db.SaveChangesAsync();

        var evt = MakeEvent("MSFT", 310m, "USD", occurredAt);

        await MarketPriceUpdatedHandler.Handle(evt, _db, _clock, _logger, CancellationToken.None);

        existing.PriceUsd.Should().Be(310m);
        existing.SyncedAt.Should().Be(FixedNow);

        // Only the existing entry in tracker, no new Add
        _db.ChangeTracker.Entries<PriceSnapshotReadModel>()
            .Count()
            .Should().Be(1);
    }

    [Fact]
    public async Task Different_date_for_same_symbol_creates_new_snapshot()
    {
        var date2 = new DateTime(2025, 5, 10, 9, 0, 0, DateTimeKind.Utc);

        var existing = new PriceSnapshotReadModel
        {
            Symbol   = "GOOG",
            Date     = new LocalDate(2025, 5, 9),
            PriceUsd = 120m,
            SyncedAt = FixedNow,
        };
        _db.PriceSnapshots.Add(existing);
        await _db.SaveChangesAsync();

        var evt = MakeEvent("GOOG", 122m, "USD", date2);

        await MarketPriceUpdatedHandler.Handle(evt, _db, _clock, _logger, CancellationToken.None);

        var added = _db.ChangeTracker.Entries<PriceSnapshotReadModel>()
            .Where(e => e.State == EntityState.Added)
            .Select(e => e.Entity)
            .Single();

        added.Symbol.Should().Be("GOOG");
        added.Date.Should().Be(new LocalDate(2025, 5, 10));
        added.PriceUsd.Should().Be(122m);
    }
}

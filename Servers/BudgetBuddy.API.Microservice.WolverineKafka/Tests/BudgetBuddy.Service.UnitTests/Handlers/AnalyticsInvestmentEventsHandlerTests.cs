using BudgetBuddy.Service.Analytics.Messaging.Handlers;
using BudgetBuddy.Service.Analytics.ReadModels;
using BudgetBuddy.Shared.Kernel.Enums;
using BudgetBuddy.Shared.Messages.Events.Investments;
using FluentAssertions;
using NodaTime;
using NSubstitute;
using Xunit;

namespace BudgetBuddy.Service.UnitTests.Handlers;

public class AnalyticsInvestmentEventsHandlerTests
{
    private readonly IInvestmentReadModelRepository _repo =
        Substitute.For<IInvestmentReadModelRepository>();

    private static readonly Guid     InvestmentId  = Guid.NewGuid();
    private static readonly LocalDate PurchaseDate = new(2025, 1, 15);

    // ── InvestmentCreatedEvent ───────────────────────────────────────────────────

    [Fact]
    public async Task Created_upserts_investment_read_model_with_correct_fields()
    {
        var evt = new InvestmentCreatedEvent
        {
            InvestmentId  = InvestmentId,
            UserId        = "user-1",
            Symbol        = "AAPL",
            Name          = "Apple Inc.",
            Type          = InvestmentType.Stock,
            Quantity      = 10m,
            PurchasePrice = 150m,
            CurrencyCode  = "USD",
            PurchaseDate  = PurchaseDate,
        };

        await InvestmentEventsHandler.Handle(evt, _repo, CancellationToken.None);

        await _repo.Received(1).UpsertAsync(
            Arg.Is<InvestmentReadModel>(m =>
                m.InvestmentId  == InvestmentId        &&
                m.UserId        == "user-1"            &&
                m.Symbol        == "AAPL"              &&
                m.Name          == "Apple Inc."        &&
                m.Type          == InvestmentType.Stock &&
                m.Quantity      == 10m                 &&
                m.PurchasePrice == 150m                &&
                m.CurrencyCode  == "USD"               &&
                m.PurchaseDate  == PurchaseDate),
            CancellationToken.None);
    }

    [Fact]
    public async Task Created_does_not_call_DeleteAsync()
    {
        var evt = new InvestmentCreatedEvent
        {
            InvestmentId = InvestmentId,
            UserId       = "user-1",
            Symbol       = "MSFT",
            Name         = "Microsoft",
            Type         = InvestmentType.Stock,
            PurchaseDate = PurchaseDate,
        };

        await InvestmentEventsHandler.Handle(evt, _repo, CancellationToken.None);

        await _repo.DidNotReceiveWithAnyArgs().DeleteAsync(Guid.Empty, default);
    }

    // ── InvestmentUpdatedEvent ───────────────────────────────────────────────────

    [Fact]
    public async Task Updated_upserts_investment_read_model_with_new_quantity()
    {
        var evt = new InvestmentUpdatedEvent
        {
            InvestmentId  = InvestmentId,
            UserId        = "user-1",
            Symbol        = "AAPL",
            Name          = "Apple Inc.",
            Type          = InvestmentType.Stock,
            Quantity      = 20m,
            PurchasePrice = 155m,
            CurrencyCode  = "USD",
            PurchaseDate  = PurchaseDate,
        };

        await InvestmentEventsHandler.Handle(evt, _repo, CancellationToken.None);

        await _repo.Received(1).UpsertAsync(
            Arg.Is<InvestmentReadModel>(m =>
                m.InvestmentId == InvestmentId &&
                m.Quantity     == 20m          &&
                m.PurchasePrice == 155m),
            CancellationToken.None);
    }

    // ── InvestmentDeletedEvent ───────────────────────────────────────────────────

    [Fact]
    public async Task Deleted_calls_DeleteAsync_with_correct_id()
    {
        var evt = new InvestmentDeletedEvent { InvestmentId = InvestmentId, UserId = "user-1" };

        await InvestmentEventsHandler.Handle(evt, _repo, CancellationToken.None);

        await _repo.Received(1).DeleteAsync(InvestmentId, CancellationToken.None);
    }

    [Fact]
    public async Task Deleted_does_not_call_UpsertAsync()
    {
        var evt = new InvestmentDeletedEvent { InvestmentId = InvestmentId, UserId = "user-1" };

        await InvestmentEventsHandler.Handle(evt, _repo, CancellationToken.None);

        await _repo.DidNotReceiveWithAnyArgs().UpsertAsync(default!, default);
    }
}

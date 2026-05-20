using BudgetBuddy.Service.Budgets.Messaging.Handlers;
using BudgetBuddy.Service.Budgets.ReadModels;
using BudgetBuddy.Shared.Kernel.Enums;
using BudgetBuddy.Shared.Messages.Events.Transactions;
using NodaTime;
using NSubstitute;
using Xunit;

namespace BudgetBuddy.Service.UnitTests.Handlers;

/// <summary>
/// Tests for TransactionSpendingHandler in the Budgets service.
/// Only expense transactions with a CategoryId update the CategorySpendingAggregate.
/// </summary>
public class TransactionSpendingHandlerTests
{
    private readonly ICategorySpendingRepository _repo =
        Substitute.For<ICategorySpendingRepository>();

    private static readonly string UserId     = "user-1";
    private static readonly Guid   CategoryId = Guid.NewGuid();
    private static readonly Guid   AccountId  = Guid.NewGuid();
    private static readonly LocalDate Date     = new(2025, 5, 15);

    // ── TransactionCreatedEvent ──────────────────────────────────────────────────

    [Fact]
    public async Task Created_expense_with_category_adds_to_expense()
    {
        var evt = BuildCreatedEvent(TransactionType.Expense, 400m, CategoryId);

        await TransactionSpendingHandler.Handle(evt, _repo, CancellationToken.None);

        await _repo.Received(1).AddToExpenseAsync(
            UserId, CategoryId, Date.Year, Date.Month, "HUF", 400m, CancellationToken.None);
    }

    [Fact]
    public async Task Created_expense_without_category_does_not_call_repository()
    {
        var evt = BuildCreatedEvent(TransactionType.Expense, 400m, categoryId: null);

        await TransactionSpendingHandler.Handle(evt, _repo, CancellationToken.None);

        await _repo.DidNotReceiveWithAnyArgs().AddToExpenseAsync(
            default!, Guid.Empty, default, default, default!, default, default);
    }

    [Fact]
    public async Task Created_income_does_not_call_repository()
    {
        var evt = BuildCreatedEvent(TransactionType.Income, 400m, CategoryId);

        await TransactionSpendingHandler.Handle(evt, _repo, CancellationToken.None);

        await _repo.DidNotReceiveWithAnyArgs().AddToExpenseAsync(
            default!, Guid.Empty, default, default, default!, default, default);
    }

    // ── TransactionUpdatedEvent ──────────────────────────────────────────────────

    [Fact]
    public async Task Updated_expense_with_positive_delta_adds_delta()
    {
        var evt = BuildUpdatedEvent(TransactionType.Expense, newAmount: 600m, oldAmount: 400m, CategoryId);

        await TransactionSpendingHandler.Handle(evt, _repo, CancellationToken.None);

        await _repo.Received(1).AddToExpenseAsync(
            UserId, CategoryId, Date.Year, Date.Month, "HUF", 200m, CancellationToken.None);
    }

    [Fact]
    public async Task Updated_expense_with_negative_delta_subtracts_delta()
    {
        var evt = BuildUpdatedEvent(TransactionType.Expense, newAmount: 200m, oldAmount: 500m, CategoryId);

        await TransactionSpendingHandler.Handle(evt, _repo, CancellationToken.None);

        await _repo.Received(1).AddToExpenseAsync(
            UserId, CategoryId, Date.Year, Date.Month, "HUF", -300m, CancellationToken.None);
    }

    [Fact]
    public async Task Updated_expense_amount_unchanged_does_not_call_repository()
    {
        var evt = BuildUpdatedEvent(TransactionType.Expense, newAmount: 300m, oldAmount: 300m, CategoryId);

        await TransactionSpendingHandler.Handle(evt, _repo, CancellationToken.None);

        await _repo.DidNotReceiveWithAnyArgs().AddToExpenseAsync(
            default!, Guid.Empty, default, default, default!, default, default);
    }

    [Fact]
    public async Task Updated_expense_without_category_does_not_call_repository()
    {
        var evt = BuildUpdatedEvent(TransactionType.Expense, newAmount: 600m, oldAmount: 400m, categoryId: null);

        await TransactionSpendingHandler.Handle(evt, _repo, CancellationToken.None);

        await _repo.DidNotReceiveWithAnyArgs().AddToExpenseAsync(
            default!, Guid.Empty, default, default, default!, default, default);
    }

    [Fact]
    public async Task Updated_income_does_not_call_repository()
    {
        var evt = BuildUpdatedEvent(TransactionType.Income, newAmount: 600m, oldAmount: 400m, CategoryId);

        await TransactionSpendingHandler.Handle(evt, _repo, CancellationToken.None);

        await _repo.DidNotReceiveWithAnyArgs().AddToExpenseAsync(
            default!, Guid.Empty, default, default, default!, default, default);
    }

    // ── TransactionDeletedEvent ──────────────────────────────────────────────────

    [Fact]
    public async Task Deleted_expense_with_category_subtracts_full_amount()
    {
        var evt = BuildDeletedEvent(TransactionType.Expense, 350m, CategoryId);

        await TransactionSpendingHandler.Handle(evt, _repo, CancellationToken.None);

        await _repo.Received(1).AddToExpenseAsync(
            UserId, CategoryId, Date.Year, Date.Month, "HUF", -350m, CancellationToken.None);
    }

    [Fact]
    public async Task Deleted_expense_without_category_does_not_call_repository()
    {
        var evt = BuildDeletedEvent(TransactionType.Expense, 350m, categoryId: null);

        await TransactionSpendingHandler.Handle(evt, _repo, CancellationToken.None);

        await _repo.DidNotReceiveWithAnyArgs().AddToExpenseAsync(
            default!, Guid.Empty, default, default, default!, default, default);
    }

    [Fact]
    public async Task Deleted_income_does_not_call_repository()
    {
        var evt = BuildDeletedEvent(TransactionType.Income, 350m, CategoryId);

        await TransactionSpendingHandler.Handle(evt, _repo, CancellationToken.None);

        await _repo.DidNotReceiveWithAnyArgs().AddToExpenseAsync(
            default!, Guid.Empty, default, default, default!, default, default);
    }

    // ── builders ─────────────────────────────────────────────────────────────────

    private TransactionCreatedEvent BuildCreatedEvent(TransactionType type, decimal amount, Guid? categoryId) =>
        new()
        {
            TransactionId   = Guid.NewGuid(),
            UserId          = UserId,
            AccountId       = AccountId,
            CategoryId      = categoryId,
            Amount          = amount,
            CurrencyCode    = "HUF",
            TransactionType = type,
            TransactionDate = Date,
        };

    private TransactionUpdatedEvent BuildUpdatedEvent(
        TransactionType type, decimal newAmount, decimal oldAmount, Guid? categoryId) =>
        new()
        {
            TransactionId   = Guid.NewGuid(),
            UserId          = UserId,
            AccountId       = AccountId,
            CategoryId      = categoryId,
            Amount          = newAmount,
            OldAmount       = oldAmount,
            CurrencyCode    = "HUF",
            TransactionType = type,
            TransactionDate = Date,
        };

    private TransactionDeletedEvent BuildDeletedEvent(TransactionType type, decimal amount, Guid? categoryId) =>
        new()
        {
            TransactionId   = Guid.NewGuid(),
            UserId          = UserId,
            AccountId       = AccountId,
            CategoryId      = categoryId,
            Amount          = amount,
            CurrencyCode    = "HUF",
            TransactionType = type,
            TransactionDate = Date,
        };
}

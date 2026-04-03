using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Domain.Enums;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Features.Budgets.GetBudgetVsActual;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Budgets;

public class GetBudgetVsActualHandlerTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetBudgetVsActualHandler _handler;
    private const string UserId = "user-123";

    public GetBudgetVsActualHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _handler = new GetBudgetVsActualHandler(_db, _currentUserService, NullLogger<GetBudgetVsActualHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenNoBudgets_ReturnsEmptyResponse()
    {
        var result = await _handler.Handle(new GetBudgetVsActualQuery(2024, 1), CancellationToken.None);

        result.TotalBudget.Should().Be(0);
        result.Categories.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithBudgetAndNoSpending_ShowsZeroUtilization()
    {
        var categoryId = Guid.NewGuid();
        _db.Categories.Add(new Category { Id = categoryId, UserId = UserId, Name = "Food" });
        _db.Budgets.Add(new Budget { Id = Guid.NewGuid(), UserId = UserId, CategoryId = categoryId, Name = "Food", Amount = 500m, CurrencyCode = "USD", Year = 2024, Month = 6 });
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetBudgetVsActualQuery(2024, 6), CancellationToken.None);

        result.TotalBudget.Should().Be(500m);
        result.TotalActual.Should().Be(0);
        result.Categories.Should().HaveCount(1);
        result.Categories[0].UtilizationPercentage.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithSpendingExceedingBudget_MarksAsOverBudget()
    {
        var categoryId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account { Id = accountId, UserId = UserId, Name = "Test", DefaultCurrencyCode = "USD" });
        _db.Categories.Add(new Category { Id = categoryId, UserId = UserId, Name = "Food" });
        _db.Budgets.Add(new Budget { Id = Guid.NewGuid(), UserId = UserId, CategoryId = categoryId, Name = "Food", Amount = 100m, CurrencyCode = "USD", Year = 2024, Month = 6 });
        _db.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(), UserId = UserId, AccountId = accountId, CategoryId = categoryId,
            Amount = 150m, CurrencyCode = "USD", TransactionType = TransactionType.Expense,
            TransactionDate = new LocalDate(2024, 6, 15)
        });
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetBudgetVsActualQuery(2024, 6), CancellationToken.None);

        result.Categories[0].IsOverBudget.Should().BeTrue();
        result.Categories[0].UtilizationPercentage.Should().BeGreaterThan(100);
    }

    public void Dispose() => _db.Dispose();
}

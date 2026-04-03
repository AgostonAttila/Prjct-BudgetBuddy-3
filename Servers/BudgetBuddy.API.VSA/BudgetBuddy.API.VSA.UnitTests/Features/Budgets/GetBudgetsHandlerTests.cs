using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Features.Budgets.GetBudgets;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Budgets;

public class GetBudgetsHandlerTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetBudgetsHandler _handler;
    private const string UserId = "user-123";

    public GetBudgetsHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _handler = new GetBudgetsHandler(_db, _currentUserService, NullLogger<GetBudgetsHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenNoBudgets_ReturnsEmptyList()
    {
        var result = await _handler.Handle(new GetBudgetsQuery(), CancellationToken.None);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsOnlyCurrentUserBudgets()
    {
        var categoryId = Guid.NewGuid();
        _db.Categories.Add(new Category { Id = categoryId, UserId = UserId, Name = "Food" });
        _db.Budgets.AddRange(
            new Budget { Id = Guid.NewGuid(), UserId = UserId, CategoryId = categoryId, Name = "Mine", Amount = 100, CurrencyCode = "USD", Year = 2024, Month = 1 },
            new Budget { Id = Guid.NewGuid(), UserId = "other-user", CategoryId = categoryId, Name = "Theirs", Amount = 200, CurrencyCode = "USD", Year = 2024, Month = 1 }
        );
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetBudgetsQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Mine");
    }

    [Fact]
    public async Task Handle_FiltersByYearAndMonth()
    {
        var categoryId = Guid.NewGuid();
        _db.Categories.Add(new Category { Id = categoryId, UserId = UserId, Name = "Food" });
        _db.Budgets.AddRange(
            new Budget { Id = Guid.NewGuid(), UserId = UserId, CategoryId = categoryId, Name = "Jan", Amount = 100, CurrencyCode = "USD", Year = 2024, Month = 1 },
            new Budget { Id = Guid.NewGuid(), UserId = UserId, CategoryId = categoryId, Name = "Feb", Amount = 200, CurrencyCode = "USD", Year = 2024, Month = 2 }
        );
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetBudgetsQuery(Year: 2024, Month: 1), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Jan");
    }

    public void Dispose() => _db.Dispose();
}

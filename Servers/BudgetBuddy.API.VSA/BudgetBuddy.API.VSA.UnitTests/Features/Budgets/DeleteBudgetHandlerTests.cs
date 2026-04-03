using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Exceptions;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Common.Shared.Services;
using BudgetBuddy.API.VSA.Features.Budgets.DeleteBudget;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Budgets;

public class DeleteBudgetHandlerTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUserCacheInvalidator _cacheInvalidator = Substitute.For<IUserCacheInvalidator>();
    private readonly DeleteBudgetHandler _handler;
    private const string UserId = "user-123";

    public DeleteBudgetHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _handler = new DeleteBudgetHandler(_db, _currentUserService, _cacheInvalidator, NullLogger<DeleteBudgetHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenBudgetExists_DeletesIt()
    {
        var budgetId = Guid.NewGuid();
        _db.Budgets.Add(new Budget { Id = budgetId, UserId = UserId, Name = "Test", Amount = 100, CurrencyCode = "USD", Year = 2024, Month = 1 });
        await _db.SaveChangesAsync();

        await _handler.Handle(new DeleteBudgetCommand(budgetId), CancellationToken.None);

        _db.Budgets.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenBudgetNotFound_ThrowsNotFoundException()
    {
        var act = () => _handler.Handle(new DeleteBudgetCommand(Guid.NewGuid()), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenBudgetBelongsToOtherUser_ThrowsNotFoundException()
    {
        var budgetId = Guid.NewGuid();
        _db.Budgets.Add(new Budget { Id = budgetId, UserId = "other-user", Name = "Test", Amount = 100, CurrencyCode = "USD", Year = 2024, Month = 1 });
        await _db.SaveChangesAsync();

        var act = () => _handler.Handle(new DeleteBudgetCommand(budgetId), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    public void Dispose() => _db.Dispose();
}

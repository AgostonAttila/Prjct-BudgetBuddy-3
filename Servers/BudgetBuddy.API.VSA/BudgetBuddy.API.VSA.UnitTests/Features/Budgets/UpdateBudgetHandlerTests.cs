using MapsterMapper;
using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Exceptions;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Common.Shared.Services;
using BudgetBuddy.API.VSA.Features.Budgets.UpdateBudget;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Budgets;

public class UpdateBudgetHandlerTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly IUserCacheInvalidator _cacheInvalidator = Substitute.For<IUserCacheInvalidator>();
    private readonly UpdateBudgetHandler _handler;
    private const string UserId = "user-123";

    public UpdateBudgetHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _mapper.Map<BudgetResponse>(Arg.Any<Budget>()).Returns(callInfo =>
        {
            var b = callInfo.ArgAt<Budget>(0);
            return new BudgetResponse(b.Id, b.Name, b.CategoryId, "", b.Amount, b.CurrencyCode, b.Year, b.Month);
        });
        _handler = new UpdateBudgetHandler(_db, _currentUserService, _cacheInvalidator, NullLogger<UpdateBudgetHandler>.Instance, _mapper);
    }

    [Fact]
    public async Task Handle_WhenBudgetNotFound_ThrowsNotFoundException()
    {
        var act = () => _handler.Handle(new UpdateBudgetCommand(Guid.NewGuid(), "New Name", 200m), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenBudgetExists_UpdatesNameAndAmount()
    {
        var budgetId = Guid.NewGuid();
        _db.Budgets.Add(new Budget { Id = budgetId, UserId = UserId, Name = "Old Name", Amount = 100, CurrencyCode = "USD", Year = 2024, Month = 1 });
        await _db.SaveChangesAsync();

        await _handler.Handle(new UpdateBudgetCommand(budgetId, "New Name", 250m), CancellationToken.None);

        var updated = _db.Budgets.Single();
        updated.Name.Should().Be("New Name");
        updated.Amount.Should().Be(250m);
    }

    public void Dispose() => _db.Dispose();
}

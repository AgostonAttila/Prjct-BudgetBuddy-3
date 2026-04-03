using MapsterMapper;
using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Exceptions;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Common.Shared.Services;
using BudgetBuddy.API.VSA.Features.Budgets.CreateBudget;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using BudgetBuddy.API.VSA.Common.Domain.Exceptions;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Budgets;

public class CreateBudgetHandlerTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly IUserCacheInvalidator _cacheInvalidator = Substitute.For<IUserCacheInvalidator>();
    private readonly CreateBudgetHandler _handler;
    private const string UserId = "user-123";

    public CreateBudgetHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _mapper.Map<Budget>(Arg.Any<CreateBudgetCommand>()).Returns(callInfo =>
        {
            var cmd = callInfo.ArgAt<CreateBudgetCommand>(0);
            return new Budget { Name = cmd.Name, Amount = cmd.Amount, CurrencyCode = cmd.CurrencyCode, Year = cmd.Year, Month = cmd.Month, CategoryId = cmd.CategoryId };
        });
        _mapper.Map<BudgetResponse>(Arg.Any<Budget>()).Returns(callInfo =>
        {
            var b = callInfo.ArgAt<Budget>(0);
            return new BudgetResponse(b.Id, b.Name, b.CategoryId, b.Category?.Name ?? "", b.Amount, b.CurrencyCode, b.Year, b.Month);
        });
        _handler = new CreateBudgetHandler(_db, _mapper, _currentUserService, _cacheInvalidator, NullLogger<CreateBudgetHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenCategoryNotFound_ThrowsNotFoundException()
    {
        var command = new CreateBudgetCommand("Groceries", Guid.NewGuid(), 100m, "USD", 2024, 1);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenBudgetAlreadyExistsForPeriod_ThrowsValidationException()
    {
        var categoryId = Guid.NewGuid();
        _db.Categories.Add(new Category { Id = categoryId, UserId = UserId, Name = "Food" });
        _db.Budgets.Add(new Budget { Id = Guid.NewGuid(), UserId = UserId, CategoryId = categoryId, Name = "Food Budget", Amount = 100, CurrencyCode = "USD", Year = 2024, Month = 1 });
        await _db.SaveChangesAsync();

        var command = new CreateBudgetCommand("Food Budget 2", categoryId, 200m, "USD", 2024, 1);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_WhenValid_CreatesBudgetInDatabase()
    {
        var categoryId = Guid.NewGuid();
        _db.Categories.Add(new Category { Id = categoryId, UserId = UserId, Name = "Food" });
        await _db.SaveChangesAsync();

        await _handler.Handle(new CreateBudgetCommand("Food Budget", categoryId, 300m, "USD", 2024, 6), CancellationToken.None);

        _db.Budgets.Should().HaveCount(1);
        _db.Budgets.Single().UserId.Should().Be(UserId);
    }

    public void Dispose() => _db.Dispose();
}

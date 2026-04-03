using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Exceptions;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Features.Categories.DeleteCategory;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Categories;

public class DeleteCategoryHandlerTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly DeleteCategoryHandler _handler;
    private const string UserId = "user-123";

    public DeleteCategoryHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _handler = new DeleteCategoryHandler(_db, _currentUserService, NullLogger<DeleteCategoryHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenCategoryExists_DeletesIt()
    {
        var id = Guid.NewGuid();
        _db.Categories.Add(new Category { Id = id, UserId = UserId, Name = "Food" });
        await _db.SaveChangesAsync();

        await _handler.Handle(new DeleteCategoryCommand(id), CancellationToken.None);

        _db.Categories.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenCategoryNotFound_ThrowsNotFoundException()
    {
        var act = () => _handler.Handle(new DeleteCategoryCommand(Guid.NewGuid()), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenCategoryBelongsToOtherUser_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _db.Categories.Add(new Category { Id = id, UserId = "other-user", Name = "Food" });
        await _db.SaveChangesAsync();

        var act = () => _handler.Handle(new DeleteCategoryCommand(id), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    public void Dispose() => _db.Dispose();
}

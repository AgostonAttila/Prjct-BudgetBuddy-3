using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Exceptions;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Features.CategoryTypes.DeleteCategoryType;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.CategoryTypes;

public class DeleteCategoryTypeHandlerTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly DeleteCategoryTypeHandler _handler;
    private const string UserId = "user-123";

    public DeleteCategoryTypeHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _handler = new DeleteCategoryTypeHandler(_db, _currentUserService, NullLogger<DeleteCategoryTypeHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenTypeExists_DeletesIt()
    {
        var categoryId = Guid.NewGuid();
        var typeId = Guid.NewGuid();
        _db.Categories.Add(new Category { Id = categoryId, UserId = UserId, Name = "Food" });
        _db.CategoryTypes.Add(new CategoryType { Id = typeId, CategoryId = categoryId, Name = "Fast Food" });
        await _db.SaveChangesAsync();

        await _handler.Handle(new DeleteCategoryTypeCommand(typeId), CancellationToken.None);

        _db.CategoryTypes.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenTypeNotFound_ThrowsNotFoundException()
    {
        var act = () => _handler.Handle(new DeleteCategoryTypeCommand(Guid.NewGuid()), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenTypeBelongsToOtherUser_ThrowsUnauthorizedAccessException()
    {
        var categoryId = Guid.NewGuid();
        var typeId = Guid.NewGuid();
        _db.Categories.Add(new Category { Id = categoryId, UserId = "other-user", Name = "Food" });
        _db.CategoryTypes.Add(new CategoryType { Id = typeId, CategoryId = categoryId, Name = "Fast Food" });
        await _db.SaveChangesAsync();

        var act = () => _handler.Handle(new DeleteCategoryTypeCommand(typeId), CancellationToken.None);
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    public void Dispose() => _db.Dispose();
}

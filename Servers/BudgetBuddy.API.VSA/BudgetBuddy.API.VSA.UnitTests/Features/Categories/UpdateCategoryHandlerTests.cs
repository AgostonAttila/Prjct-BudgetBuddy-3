using MapsterMapper;
using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Exceptions;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Features.Categories.UpdateCategory;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Categories;

public class UpdateCategoryHandlerTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly UpdateCategoryHandler _handler;
    private const string UserId = "user-123";

    public UpdateCategoryHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _mapper.Map<CategoryResponse>(Arg.Any<Category>()).Returns(callInfo =>
        {
            var c = callInfo.ArgAt<Category>(0);
            return new CategoryResponse(c.Id, c.Name, c.Icon, c.Color);
        });
        _handler = new UpdateCategoryHandler(_db, _mapper, _currentUserService, NullLogger<UpdateCategoryHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenCategoryNotFound_ThrowsNotFoundException()
    {
        var act = () => _handler.Handle(new UpdateCategoryCommand(Guid.NewGuid(), "New Name", null, null), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenCategoryExists_UpdatesFields()
    {
        var id = Guid.NewGuid();
        _db.Categories.Add(new Category { Id = id, UserId = UserId, Name = "Old Name" });
        await _db.SaveChangesAsync();

        await _handler.Handle(new UpdateCategoryCommand(id, "New Name", "🎯", "#0000FF"), CancellationToken.None);

        var updated = _db.Categories.Single();
        updated.Name.Should().Be("New Name");
        updated.Icon.Should().Be("🎯");
        updated.Color.Should().Be("#0000FF");
    }

    [Fact]
    public async Task Handle_WhenBelongsToOtherUser_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _db.Categories.Add(new Category { Id = id, UserId = "other-user", Name = "Other" });
        await _db.SaveChangesAsync();

        var act = () => _handler.Handle(new UpdateCategoryCommand(id, "New Name", null, null), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    public void Dispose() => _db.Dispose();
}

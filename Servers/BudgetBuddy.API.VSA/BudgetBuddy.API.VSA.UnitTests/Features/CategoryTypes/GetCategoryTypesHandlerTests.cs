using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Features.CategoryTypes.GetCategoryTypes;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.CategoryTypes;

public class GetCategoryTypesHandlerTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetCategoryTypesHandler _handler;
    private const string UserId = "user-123";

    public GetCategoryTypesHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _handler = new GetCategoryTypesHandler(_db, _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenNoTypes_ReturnsEmpty()
    {
        var result = await _handler.Handle(new GetCategoryTypesQuery(null), CancellationToken.None);
        result.CategoryTypes.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsOnlyCurrentUserTypes()
    {
        var myCategory = Guid.NewGuid();
        var otherCategory = Guid.NewGuid();
        _db.Categories.AddRange(
            new Category { Id = myCategory, UserId = UserId, Name = "Mine" },
            new Category { Id = otherCategory, UserId = "other-user", Name = "Other" }
        );
        _db.CategoryTypes.AddRange(
            new CategoryType { Id = Guid.NewGuid(), CategoryId = myCategory, Name = "My Type" },
            new CategoryType { Id = Guid.NewGuid(), CategoryId = otherCategory, Name = "Other Type" }
        );
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetCategoryTypesQuery(null), CancellationToken.None);

        result.CategoryTypes.Should().HaveCount(1);
        result.CategoryTypes[0].Name.Should().Be("My Type");
    }

    [Fact]
    public async Task Handle_FiltersByCategoryId()
    {
        var cat1 = Guid.NewGuid();
        var cat2 = Guid.NewGuid();
        _db.Categories.AddRange(
            new Category { Id = cat1, UserId = UserId, Name = "Food" },
            new Category { Id = cat2, UserId = UserId, Name = "Transport" }
        );
        _db.CategoryTypes.AddRange(
            new CategoryType { Id = Guid.NewGuid(), CategoryId = cat1, Name = "Restaurant" },
            new CategoryType { Id = Guid.NewGuid(), CategoryId = cat2, Name = "Bus" }
        );
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetCategoryTypesQuery(CategoryId: cat1), CancellationToken.None);

        result.CategoryTypes.Should().HaveCount(1);
        result.CategoryTypes[0].Name.Should().Be("Restaurant");
    }

    public void Dispose() => _db.Dispose();
}

using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Features.Categories.GetCategories;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Categories;

public class GetCategoriesHandlerTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetCategoriesHandler _handler;
    private const string UserId = "user-123";

    public GetCategoriesHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _handler = new GetCategoriesHandler(_db, new PassthroughHybridCache(), _currentUserService, NullLogger<GetCategoriesHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenNoCategories_ReturnsEmptyList()
    {
        var result = await _handler.Handle(new GetCategoriesQuery(), CancellationToken.None);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsOnlyCurrentUserCategories()
    {
        _db.Categories.AddRange(
            new Category { Id = Guid.NewGuid(), UserId = UserId, Name = "Food" },
            new Category { Id = Guid.NewGuid(), UserId = "other-user", Name = "Travel" }
        );
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Food");
    }

    [Fact]
    public async Task Handle_ReturnsCorrectCategoryDtoFields()
    {
        _db.Categories.Add(new Category { Id = Guid.NewGuid(), UserId = UserId, Name = "Food", Icon = "🍕", Color = "#FF0000" });
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

        result[0].Icon.Should().Be("🍕");
        result[0].Color.Should().Be("#FF0000");
    }

    public void Dispose() => _db.Dispose();
}

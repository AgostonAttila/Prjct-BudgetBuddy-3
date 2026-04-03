using MapsterMapper;
using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Features.Categories.CreateCategory;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Categories;

public class CreateCategoryHandlerTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly CreateCategoryHandler _handler;
    private const string UserId = "user-123";

    public CreateCategoryHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _mapper.Map<Category>(Arg.Any<CreateCategoryCommand>()).Returns(callInfo =>
        {
            var cmd = callInfo.ArgAt<CreateCategoryCommand>(0);
            return new Category { Name = cmd.Name, Icon = cmd.Icon, Color = cmd.Color };
        });
        _mapper.Map<CreateCategoryResponse>(Arg.Any<Category>()).Returns(callInfo =>
        {
            var c = callInfo.ArgAt<Category>(0);
            return new CreateCategoryResponse(c.Id, c.Name, c.Icon, c.Color);
        });
        _handler = new CreateCategoryHandler(_db, _mapper, _currentUserService, NullLogger<CreateCategoryHandler>.Instance);
    }

    [Fact]
    public async Task Handle_SavesCategoryToDatabase()
    {
        var command = new CreateCategoryCommand("Food", "🍕", "#FF0000");

        await _handler.Handle(command, CancellationToken.None);

        _db.Categories.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_SetsUserIdFromCurrentUser()
    {
        await _handler.Handle(new CreateCategoryCommand("Food", null, null), CancellationToken.None);

        _db.Categories.Single().UserId.Should().Be(UserId);
    }

    [Fact]
    public async Task Handle_ReturnsMappedResponse()
    {
        var result = await _handler.Handle(new CreateCategoryCommand("Food", null, null), CancellationToken.None);

        result.Should().NotBeNull();
        result.Name.Should().Be("Food");
    }

    public void Dispose() => _db.Dispose();
}

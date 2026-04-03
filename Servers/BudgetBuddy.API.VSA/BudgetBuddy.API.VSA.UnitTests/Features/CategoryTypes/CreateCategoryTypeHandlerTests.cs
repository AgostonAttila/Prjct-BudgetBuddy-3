using MapsterMapper;
using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Exceptions;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Features.CategoryTypes.CreateCategoryType;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.CategoryTypes;

public class CreateCategoryTypeHandlerTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly CreateCategoryTypeHandler _handler;
    private const string UserId = "user-123";

    public CreateCategoryTypeHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _mapper.Map<CategoryType>(Arg.Any<CreateCategoryTypeCommand>()).Returns(callInfo =>
        {
            var cmd = callInfo.ArgAt<CreateCategoryTypeCommand>(0);
            return new CategoryType { Name = cmd.Name, Icon = cmd.Icon, Color = cmd.Color, CategoryId = cmd.CategoryId };
        });
        _mapper.Map<CreateCategoryTypeResponse>(Arg.Any<CategoryType>()).Returns(callInfo =>
        {
            var ct = callInfo.ArgAt<CategoryType>(0);
            return new CreateCategoryTypeResponse(ct.Id, ct.CategoryId, ct.Name, ct.Icon, ct.Color);
        });
        _handler = new CreateCategoryTypeHandler(_db, _currentUserService, _mapper, NullLogger<CreateCategoryTypeHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenCategoryNotFound_ThrowsNotFoundException()
    {
        var act = () => _handler.Handle(new CreateCategoryTypeCommand(Guid.NewGuid(), "Sub", null, null), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenCategoryBelongsToOtherUser_ThrowsNotFoundException()
    {
        var categoryId = Guid.NewGuid();
        _db.Categories.Add(new Category { Id = categoryId, UserId = "other-user", Name = "Food" });
        await _db.SaveChangesAsync();

        var act = () => _handler.Handle(new CreateCategoryTypeCommand(categoryId, "Sub", null, null), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenCategoryExists_CreatesCategoryType()
    {
        var categoryId = Guid.NewGuid();
        _db.Categories.Add(new Category { Id = categoryId, UserId = UserId, Name = "Food" });
        await _db.SaveChangesAsync();

        await _handler.Handle(new CreateCategoryTypeCommand(categoryId, "Fast Food", null, null), CancellationToken.None);

        _db.CategoryTypes.Should().HaveCount(1);
        _db.CategoryTypes.Single().CategoryId.Should().Be(categoryId);
    }

    public void Dispose() => _db.Dispose();
}

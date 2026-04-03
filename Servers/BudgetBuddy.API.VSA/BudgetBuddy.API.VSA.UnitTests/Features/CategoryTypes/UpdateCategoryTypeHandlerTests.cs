using MapsterMapper;
using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Exceptions;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Features.CategoryTypes.UpdateCategoryType;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.CategoryTypes;

public class UpdateCategoryTypeHandlerTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly UpdateCategoryTypeHandler _handler;
    private const string UserId = "user-123";

    public UpdateCategoryTypeHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _mapper.When(m => m.Map(Arg.Any<UpdateCategoryTypeCommand>(), Arg.Any<CategoryType>()))
            .Do(callInfo =>
            {
                var cmd = callInfo.ArgAt<UpdateCategoryTypeCommand>(0);
                var ct = callInfo.ArgAt<CategoryType>(1);
                ct.Name = cmd.Name;
                ct.Icon = cmd.Icon;
                ct.Color = cmd.Color;
            });
        _handler = new UpdateCategoryTypeHandler(_db, _currentUserService, _mapper, NullLogger<UpdateCategoryTypeHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenTypeNotFound_ThrowsNotFoundException()
    {
        var act = () => _handler.Handle(new UpdateCategoryTypeCommand(Guid.NewGuid(), "New Name", null, null), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenTypeExists_UpdatesName()
    {
        var categoryId = Guid.NewGuid();
        var typeId = Guid.NewGuid();
        _db.Categories.Add(new Category { Id = categoryId, UserId = UserId, Name = "Food" });
        _db.CategoryTypes.Add(new CategoryType { Id = typeId, CategoryId = categoryId, Name = "Old Name" });
        await _db.SaveChangesAsync();

        await _handler.Handle(new UpdateCategoryTypeCommand(typeId, "New Name", null, null), CancellationToken.None);

        _db.CategoryTypes.Single().Name.Should().Be("New Name");
    }

    public void Dispose() => _db.Dispose();
}

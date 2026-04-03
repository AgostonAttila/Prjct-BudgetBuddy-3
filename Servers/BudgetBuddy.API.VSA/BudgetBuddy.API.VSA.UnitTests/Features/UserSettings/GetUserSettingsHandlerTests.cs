using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Exceptions;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Features.UserSettings.GetUserSettings;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.UserSettings;

public class GetUserSettingsHandlerTests
{
    private readonly UserManager<User> _userManager = Substitute.For<UserManager<User>>(
        Substitute.For<IUserStore<User>>(), null, null, null, null, null, null, null, null);
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetUserSettingsHandler _handler;
    private const string UserId = "user-123";

    public GetUserSettingsHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _handler = new GetUserSettingsHandler(_userManager, _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenUserFound_ReturnsUserSettings()
    {
        var user = new User
        {
            Id = UserId,
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            PreferredLanguage = "en-US",
            DefaultCurrency = "USD",
            DateFormat = "yyyy-MM-dd"
        };
        _userManager.FindByIdAsync(UserId).Returns(user);

        var result = await _handler.Handle(new GetUserSettingsQuery(), CancellationToken.None);

        result.UserId.Should().Be(UserId);
        result.Email.Should().Be("test@example.com");
        result.PreferredLanguage.Should().Be("en-US");
        result.DefaultCurrency.Should().Be("USD");
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsNotFoundException()
    {
        _userManager.FindByIdAsync(UserId).Returns((User?)null);

        var act = () => _handler.Handle(new GetUserSettingsQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

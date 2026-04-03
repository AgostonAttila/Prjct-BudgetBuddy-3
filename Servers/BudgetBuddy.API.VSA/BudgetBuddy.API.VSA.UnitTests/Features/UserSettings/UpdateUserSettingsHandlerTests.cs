using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Exceptions;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Features.UserSettings.UpdateUserSettings;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.UserSettings;

public class UpdateUserSettingsHandlerTests
{
    private readonly UserManager<User> _userManager = Substitute.For<UserManager<User>>(
        Substitute.For<IUserStore<User>>(), null, null, null, null, null, null, null, null);
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly UpdateUserSettingsHandler _handler;
    private const string UserId = "user-123";

    public UpdateUserSettingsHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _handler = new UpdateUserSettingsHandler(_userManager, _currentUserService, NullLogger<UpdateUserSettingsHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsNotFoundException()
    {
        _userManager.FindByIdAsync(UserId).Returns((User?)null);

        var act = () => _handler.Handle(new UpdateUserSettingsCommand(null, null, "en-US", "USD", "yyyy-MM-dd"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenUpdateSucceeds_ReturnsSuccessResponse()
    {
        var user = new User
        {
            Id = UserId,
            Email = "test@example.com",
            PreferredLanguage = "en-US",
            DefaultCurrency = "USD",
            DateFormat = "yyyy-MM-dd"
        };
        _userManager.FindByIdAsync(UserId).Returns(user);
        _userManager.UpdateAsync(user).Returns(IdentityResult.Success);

        var result = await _handler.Handle(new UpdateUserSettingsCommand(null, null, "hu-HU", "EUR", "dd/MM/yyyy"), CancellationToken.None);

        result.Message.Should().Be("User settings updated successfully");
    }
}

using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Infrastructure.Notification.Email;
using BudgetBuddy.API.VSA.Features.TwoFactor.DisableTwoFactor;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Security.Claims;

namespace BudgetBuddy.API.VSA.UnitTests.Features.TwoFactor;

public class DisableTwoFactorHandlerTests
{
    private readonly UserManager<User> _userManager = Substitute.For<UserManager<User>>(
        Substitute.For<IUserStore<User>>(), null, null, null, null, null, null, null, null);
    private readonly IAuthenticationEmailService _authEmailService = Substitute.For<IAuthenticationEmailService>();
    private readonly DisableTwoFactorHandler _handler;

    public DisableTwoFactorHandlerTests()
    {
        _handler = new DisableTwoFactorHandler(_userManager, _authEmailService, NullLogger<DisableTwoFactorHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsUnauthorizedAccessException()
    {
        _userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns((User?)null);

        var act = () => _handler.Handle(new DisableTwoFactorCommand(new ClaimsPrincipal(), "password"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_WhenPasswordIsInvalid_ThrowsValidationException()
    {
        var user = new User { Id = "user-123", Email = "test@example.com" };
        _userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(user);
        _userManager.CheckPasswordAsync(user, "wrong-password").Returns(false);

        var act = () => _handler.Handle(new DisableTwoFactorCommand(new ClaimsPrincipal(), "wrong-password"), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_WhenSuccessful_Returns2FADisabledResponse()
    {
        var user = new User { Id = "user-123", Email = "test@example.com", UserName = "testuser" };
        _userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(user);
        _userManager.CheckPasswordAsync(user, "correct-password").Returns(true);
        _userManager.SetTwoFactorEnabledAsync(user, false).Returns(IdentityResult.Success);
        _userManager.ResetAuthenticatorKeyAsync(user).Returns(IdentityResult.Success);
        _userManager.GetEmailAsync(user).Returns("test@example.com");

        var result = await _handler.Handle(new DisableTwoFactorCommand(new ClaimsPrincipal(), "correct-password"), CancellationToken.None);

        result.Success.Should().BeTrue();
    }
}

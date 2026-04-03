using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Features.TwoFactor.EnableTwoFactor;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Security.Claims;

namespace BudgetBuddy.API.VSA.UnitTests.Features.TwoFactor;

public class EnableTwoFactorHandlerTests
{
    private readonly UserManager<User> _userManager = Substitute.For<UserManager<User>>(
        Substitute.For<IUserStore<User>>(), null, null, null, null, null, null, null, null);
    private readonly EnableTwoFactorHandler _handler;

    public EnableTwoFactorHandlerTests()
    {
        _handler = new EnableTwoFactorHandler(_userManager, NullLogger<EnableTwoFactorHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsUnauthorizedAccessException()
    {
        _userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns((User?)null);

        var act = () => _handler.Handle(new EnableTwoFactorCommand(new ClaimsPrincipal()), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_WhenUserHasNoKey_GeneratesNewKey()
    {
        var user = new User { Id = "user-123", Email = "test@example.com" };
        _userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(user);
        _userManager.GetAuthenticatorKeyAsync(user).Returns((string?)null, "NEWKEY12345678901234567890123456");
        _userManager.GetEmailAsync(user).Returns("test@example.com");
        _userManager.ResetAuthenticatorKeyAsync(user).Returns(IdentityResult.Success);

        await _handler.Handle(new EnableTwoFactorCommand(new ClaimsPrincipal()), CancellationToken.None);

        await _userManager.Received(1).ResetAuthenticatorKeyAsync(user);
    }

    [Fact]
    public async Task Handle_ReturnsSharedKeyAndQrCode()
    {
        var user = new User { Id = "user-123", Email = "test@example.com" };
        _userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(user);
        _userManager.GetAuthenticatorKeyAsync(user).Returns("ABCDEFGHIJKLMNOP");
        _userManager.GetEmailAsync(user).Returns("test@example.com");

        var result = await _handler.Handle(new EnableTwoFactorCommand(new ClaimsPrincipal()), CancellationToken.None);

        result.SharedKey.Should().NotBeNullOrEmpty();
        result.QrCodeDataUrl.Should().StartWith("data:image/png;base64,");
    }
}

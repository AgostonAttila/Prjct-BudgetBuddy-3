using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Infrastructure.Notification.Email;
using BudgetBuddy.API.VSA.Features.TwoFactor.VerifyTwoFactorSetup;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Security.Claims;

namespace BudgetBuddy.API.VSA.UnitTests.Features.TwoFactor;

public class VerifyTwoFactorSetupHandlerTests
{
    private readonly UserManager<User> _userManager = Substitute.For<UserManager<User>>(
        Substitute.For<IUserStore<User>>(), null, null, null, null, null, null, null, null);
    private readonly IAuthenticationEmailService _authEmailService = Substitute.For<IAuthenticationEmailService>();
    private readonly VerifyTwoFactorSetupHandler _handler;

    public VerifyTwoFactorSetupHandlerTests()
    {
        _handler = new VerifyTwoFactorSetupHandler(_userManager, _authEmailService, NullLogger<VerifyTwoFactorSetupHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsUnauthorizedAccessException()
    {
        _userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns((User?)null);

        var act = () => _handler.Handle(new VerifyTwoFactorSetupCommand(new ClaimsPrincipal(), "123456"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_WhenCodeIsInvalid_ThrowsValidationException()
    {
        var user = new User { Id = "user-123" };
        _userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(user);
        _userManager.VerifyTwoFactorTokenAsync(user, Arg.Any<string>(), "000000").Returns(false);

        var act = () => _handler.Handle(new VerifyTwoFactorSetupCommand(new ClaimsPrincipal(), "000000"), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_WhenCodeIsValid_EnablesTwoFactor()
    {
        var user = new User { Id = "user-123", Email = "test@example.com", UserName = "testuser" };
        _userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(user);
        _userManager.VerifyTwoFactorTokenAsync(user, Arg.Any<string>(), "123456").Returns(true);
        _userManager.SetTwoFactorEnabledAsync(user, true).Returns(IdentityResult.Success);
        _userManager.GetEmailAsync(user).Returns("test@example.com");

        var result = await _handler.Handle(new VerifyTwoFactorSetupCommand(new ClaimsPrincipal(), "123456"), CancellationToken.None);

        result.Success.Should().BeTrue();
        await _userManager.Received(1).SetTwoFactorEnabledAsync(user, true);
    }
}

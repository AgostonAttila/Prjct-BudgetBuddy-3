using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Features.TwoFactor.GetTwoFactorStatus;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Security.Claims;

namespace BudgetBuddy.API.VSA.UnitTests.Features.TwoFactor;

public class GetTwoFactorStatusHandlerTests
{
    private readonly UserManager<User> _userManager = Substitute.For<UserManager<User>>(
        Substitute.For<IUserStore<User>>(), null, null, null, null, null, null, null, null);
    private readonly GetTwoFactorStatusHandler _handler;

    public GetTwoFactorStatusHandlerTests()
    {
        _handler = new GetTwoFactorStatusHandler(_userManager, NullLogger<GetTwoFactorStatusHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsUnauthorizedAccessException()
    {
        _userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns((User?)null);

        var act = () => _handler.Handle(new GetTwoFactorStatusQuery(new ClaimsPrincipal()), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_WhenUserFound_ReturnsTwoFactorStatus()
    {
        var user = new User { Id = "user-123", Email = "test@example.com" };
        _userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(user);
        _userManager.GetTwoFactorEnabledAsync(user).Returns(true);
        _userManager.GetAuthenticatorKeyAsync(user).Returns("SOMEKEY");

        var result = await _handler.Handle(new GetTwoFactorStatusQuery(new ClaimsPrincipal()), CancellationToken.None);

        result.IsEnabled.Should().BeTrue();
        result.HasAuthenticator.Should().BeTrue();
    }
}

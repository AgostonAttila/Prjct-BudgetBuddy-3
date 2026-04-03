using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Features.TwoFactor.GetRecoveryCodes;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace BudgetBuddy.API.VSA.UnitTests.Features.TwoFactor;

public class GetRecoveryCodesHandlerTests
{
    private readonly UserManager<User> _userManager = Substitute.For<UserManager<User>>(
        Substitute.For<IUserStore<User>>(), null, null, null, null, null, null, null, null);
    private readonly IDistributedCache _cache = Substitute.For<IDistributedCache>();
    private readonly GetRecoveryCodesHandler _handler;

    public GetRecoveryCodesHandlerTests()
    {
        _handler = new GetRecoveryCodesHandler(_userManager, _cache, NullLogger<GetRecoveryCodesHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsUnauthorizedAccessException()
    {
        _userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns((User?)null);

        var act = () => _handler.Handle(new GetRecoveryCodesQuery(new ClaimsPrincipal(), 1, true), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_WhenTwoFactorNotEnabled_ThrowsInvalidOperationException()
    {
        var user = new User { Id = "user-123" };
        _userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(user);
        _userManager.GetTwoFactorEnabledAsync(user).Returns(false);

        var act = () => _handler.Handle(new GetRecoveryCodesQuery(new ClaimsPrincipal(), 1, true), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_WhenGenerateNew_GeneratesAndReturnsFirstBatch()
    {
        var user = new User { Id = "user-123" };
        var codes = Enumerable.Range(1, 10).Select(i => $"code-{i:00}").ToArray();

        _userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(user);
        _userManager.GetTwoFactorEnabledAsync(user).Returns(true);
        _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10).Returns(codes);

        var result = await _handler.Handle(new GetRecoveryCodesQuery(new ClaimsPrincipal(), 1, true), CancellationToken.None);

        result.RecoveryCodes.Should().HaveCount(5);
        result.CurrentBatch.Should().Be(1);
        result.HasMoreBatches.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenNoCachedCodes_ThrowsInvalidOperationException()
    {
        var user = new User { Id = "user-123" };
        _userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(user);
        _userManager.GetTwoFactorEnabledAsync(user).Returns(true);
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);

        var act = () => _handler.Handle(new GetRecoveryCodesQuery(new ClaimsPrincipal(), 1, false), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}

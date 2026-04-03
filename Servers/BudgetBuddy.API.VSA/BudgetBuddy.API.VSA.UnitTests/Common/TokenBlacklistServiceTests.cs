using BudgetBuddy.API.VSA.Common.Infrastructure.Security.Authentication;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Text;

namespace BudgetBuddy.API.VSA.UnitTests.Common;

public class TokenBlacklistServiceTests
{
    private readonly IDistributedCache _cache = Substitute.For<IDistributedCache>();
    private readonly TokenBlacklistService _sut;

    public TokenBlacklistServiceTests()
    {
        _sut = new TokenBlacklistService(_cache, NullLogger<TokenBlacklistService>.Instance);
    }

    // ── RevokeTokenAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task RevokeToken_WhenFutureExpiry_WritesToCache()
    {
        await _sut.RevokeTokenAsync("some-token", DateTimeOffset.UtcNow.AddHours(1));

        await _cache.Received(1).SetAsync(
            Arg.Any<string>(),
            Arg.Any<byte[]>(),
            Arg.Any<DistributedCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RevokeToken_WhenAlreadyExpired_DoesNotWriteToCache()
    {
        await _sut.RevokeTokenAsync("expired-token", DateTimeOffset.UtcNow.AddSeconds(-1));

        await _cache.DidNotReceive().SetAsync(
            Arg.Any<string>(),
            Arg.Any<byte[]>(),
            Arg.Any<DistributedCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RevokeToken_WhenNoExpiryProvided_UsesDefaultTtl()
    {
        await _sut.RevokeTokenAsync("no-expiry-token");

        await _cache.Received(1).SetAsync(
            Arg.Any<string>(),
            Arg.Any<byte[]>(),
            Arg.Is<DistributedCacheEntryOptions>(o =>
                o.AbsoluteExpirationRelativeToNow == TimeSpan.FromHours(24)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RevokeToken_TwoDifferentTokens_UsesDifferentCacheKeys()
    {
        var capturedKeys = new List<string>();
        await _cache.SetAsync(
            Arg.Do<string>(k => capturedKeys.Add(k)),
            Arg.Any<byte[]>(),
            Arg.Any<DistributedCacheEntryOptions>(),
            Arg.Any<CancellationToken>());

        await _sut.RevokeTokenAsync("token-A", DateTimeOffset.UtcNow.AddHours(1));
        await _sut.RevokeTokenAsync("token-B", DateTimeOffset.UtcNow.AddHours(1));

        capturedKeys.Should().HaveCount(2);
        capturedKeys[0].Should().NotBe(capturedKeys[1]);
    }

    // ── IsTokenRevokedAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task IsTokenRevoked_WhenTokenInCache_ReturnsTrue()
    {
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Encoding.UTF8.GetBytes("revoked"));

        var result = await _sut.IsTokenRevokedAsync("revoked-token");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsTokenRevoked_WhenTokenNotInCache_ReturnsFalse()
    {
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        var result = await _sut.IsTokenRevokedAsync("valid-token");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsTokenRevoked_WhenCacheThrows_ReturnsTrueFailClosed()
    {
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Cache unavailable"));

        var result = await _sut.IsTokenRevokedAsync("any-token");

        result.Should().BeTrue("fail-closed: a cache hiba esetén a tokent visszavontnak kell tekinteni");
    }

    // ── RevokeAllUserTokensAsync ─────────────────────────────────────────────

    [Fact]
    public async Task RevokeAllUserTokens_WritesTimestampWithUserId()
    {
        var userId = "user-abc";

        await _sut.RevokeAllUserTokensAsync(userId);

        await _cache.Received(1).SetAsync(
            Arg.Is<string>(k => k.Contains(userId)),
            Arg.Any<byte[]>(),
            Arg.Any<DistributedCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
    }

    // ── AreAllUserTokensRevokedAsync ─────────────────────────────────────────

    [Fact]
    public async Task AreAllUserTokensRevoked_WhenTokenIssuedBeforeRevocation_ReturnsTrue()
    {
        var revokedAt = DateTimeOffset.UtcNow;
        var tokenIssuedAt = revokedAt.AddMinutes(-10);

        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Encoding.UTF8.GetBytes(revokedAt.ToString("o")));

        var result = await _sut.AreAllUserTokensRevokedAsync("user-1", tokenIssuedAt);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task AreAllUserTokensRevoked_WhenTokenIssuedAfterRevocation_ReturnsFalse()
    {
        var revokedAt = DateTimeOffset.UtcNow.AddHours(-2);
        var tokenIssuedAt = revokedAt.AddHours(1);

        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Encoding.UTF8.GetBytes(revokedAt.ToString("o")));

        var result = await _sut.AreAllUserTokensRevokedAsync("user-1", tokenIssuedAt);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task AreAllUserTokensRevoked_WhenNoRevocationEntry_ReturnsFalse()
    {
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        var result = await _sut.AreAllUserTokensRevokedAsync("user-1", DateTimeOffset.UtcNow);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task AreAllUserTokensRevoked_WhenCacheThrows_ReturnsTrueFailClosed()
    {
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Cache down"));

        var result = await _sut.AreAllUserTokensRevokedAsync("user-1", DateTimeOffset.UtcNow);

        result.Should().BeTrue("fail-closed: bizonytalan állapotban a tokent visszavontnak kell tekinteni");
    }
}

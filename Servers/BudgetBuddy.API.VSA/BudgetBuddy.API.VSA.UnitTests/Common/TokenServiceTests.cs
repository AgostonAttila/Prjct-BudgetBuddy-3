using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Infrastructure.Security.Authentication;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Common;

public class TokenServiceTests
{
    private readonly TokenService _sut;
    private readonly User _testUser;

    private const string SecretKey = "BudgetBuddy-UnitTest-SecretKey-256bits-LongEnoughForHS256!!";

    public TokenServiceTests()
    {
        var config = Substitute.For<IConfiguration>();
        config["Jwt:SecretKey"].Returns(SecretKey);
        config["Jwt:Issuer"].Returns("test-issuer");
        config["Jwt:Audience"].Returns("test-audience");

        _sut = new TokenService(config, NullLogger<TokenService>.Instance);

        _testUser = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@budgetbuddy.com",
            UserName = "testuser"
        };
    }

    // ── GenerateAccessToken ──────────────────────────────────────────────────

    [Fact]
    public void GenerateAccessToken_ReturnsNonEmptyString()
    {
        var token = _sut.GenerateAccessToken(_testUser);

        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GenerateAccessToken_ReturnsValidJwtFormat()
    {
        var token = _sut.GenerateAccessToken(_testUser);

        // JWT = header.payload.signature (3 Base64url parts)
        token.Split('.').Should().HaveCount(3);
    }

    [Fact]
    public void GenerateAccessToken_ContainsCorrectNameIdentifierClaim()
    {
        var token = _sut.GenerateAccessToken(_testUser);

        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey))
        }, out _);

        principal.FindFirstValue(ClaimTypes.NameIdentifier).Should().Be(_testUser.Id);
    }

    [Fact]
    public void GenerateAccessToken_ContainsEmailClaim()
    {
        var token = _sut.GenerateAccessToken(_testUser);

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        // ReadJwtToken returns raw JWT short names; ClaimTypes.Email maps to "email"
        var email = parsed.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value;

        email.Should().Be(_testUser.Email);
    }

    [Fact]
    public void GenerateAccessToken_ExpiresApproximately15MinutesFromNow()
    {
        var before = DateTime.UtcNow;

        var token = _sut.GenerateAccessToken(_testUser);

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        parsed.ValidTo.Should().BeCloseTo(before.AddMinutes(15), precision: TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void GenerateAccessToken_ContainsUniqueJtiOnEachCall()
    {
        var token1 = _sut.GenerateAccessToken(_testUser);
        var token2 = _sut.GenerateAccessToken(_testUser);

        var jti1 = new JwtSecurityTokenHandler().ReadJwtToken(token1)
            .Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
        var jti2 = new JwtSecurityTokenHandler().ReadJwtToken(token2)
            .Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;

        jti1.Should().NotBe(jti2, "minden token egyedi Jti-t kap replay attack elleni védelemként");
    }

    // ── GenerateRefreshTokenString ───────────────────────────────────────────

    [Fact]
    public void GenerateRefreshTokenString_ReturnsBase64Of64Bytes()
    {
        var token = _sut.GenerateRefreshTokenString();

        var bytes = Convert.FromBase64String(token);
        bytes.Should().HaveCount(64, "512-bit cryptographically secure random token");
    }

    [Fact]
    public void GenerateRefreshTokenString_IsUniqueOnEachCall()
    {
        var tokens = Enumerable.Range(0, 10).Select(_ => _sut.GenerateRefreshTokenString()).ToList();

        tokens.Distinct().Should().HaveCount(10, "minden token egyedi kell legyen");
    }

    // ── CreateRefreshToken ───────────────────────────────────────────────────

    [Fact]
    public void CreateRefreshToken_SetsCorrectUserId()
    {
        var refreshToken = _sut.CreateRefreshToken(_testUser, ipAddress: "127.0.0.1");

        refreshToken.UserId.Should().Be(_testUser.Id);
    }

    [Fact]
    public void CreateRefreshToken_ExpiresIn7Days()
    {
        var before = DateTime.UtcNow;

        var refreshToken = _sut.CreateRefreshToken(_testUser, ipAddress: null);

        refreshToken.ExpiresAt.Should().BeCloseTo(before.AddDays(7), precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void CreateRefreshToken_PlainTokenMatchesTokenField()
    {
        var refreshToken = _sut.CreateRefreshToken(_testUser, ipAddress: null);

        refreshToken.PlainToken.Should().Be(refreshToken.Token,
            "CreateRefreshToken után a plain token és a Token mező azonos – a hash a DB mentésnél fut");
    }

    [Fact]
    public void CreateRefreshToken_TokenIsNotExpiredAndNotRevoked()
    {
        var refreshToken = _sut.CreateRefreshToken(_testUser, ipAddress: null);

        refreshToken.IsExpired.Should().BeFalse();
        refreshToken.IsRevoked.Should().BeFalse();
        refreshToken.IsActive.Should().BeTrue();
    }
}

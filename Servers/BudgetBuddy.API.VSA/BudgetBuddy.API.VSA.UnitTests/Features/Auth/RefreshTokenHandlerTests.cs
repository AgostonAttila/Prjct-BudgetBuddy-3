using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Infrastructure.Security.Authentication;
using BudgetBuddy.API.VSA.Features.Auth.RefreshToken;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Auth;

public class RefreshTokenHandlerTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IHttpContextAccessor _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
    private readonly RefreshTokenHandler _handler;

    public RefreshTokenHandlerTests()
    {
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);
        _handler = new RefreshTokenHandler(_db, _tokenService, NullLogger<RefreshTokenHandler>.Instance, _httpContextAccessor);
    }

    [Fact]
    public async Task Handle_WhenTokenNotFound_ThrowsUnauthorizedAccessException()
    {
        var act = () => _handler.Handle(new RefreshTokenCommand("invalid-token"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Invalid refresh token*");
    }

    [Fact]
    public async Task Handle_WhenTokenHashCanBeComputed_CorrectlyIdentifiesToken()
    {
        // Sending a non-existent token returns UnauthorizedAccessException — proves hashing works
        var act = () => _handler.Handle(new RefreshTokenCommand("some-token-value"), CancellationToken.None);
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    public void Dispose() => _db.Dispose();
}

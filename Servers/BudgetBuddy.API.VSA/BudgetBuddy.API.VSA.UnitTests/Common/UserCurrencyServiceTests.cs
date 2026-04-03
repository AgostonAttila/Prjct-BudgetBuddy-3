using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Common;

public class UserCurrencyServiceTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly UserCurrencyService _sut;

    private const string UserId = "user-currency-123";

    public UserCurrencyServiceTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _sut = new UserCurrencyService(_currentUserService, _db);
    }

    [Fact]
    public async Task GetDisplayCurrency_WhenRequestedCurrencyProvided_ReturnsItUppercased()
    {
        var result = await _sut.GetDisplayCurrencyAsync("eur");

        result.Should().Be("EUR");
    }

    [Fact]
    public async Task GetDisplayCurrency_WhenRequestedCurrencyIsAlreadyUppercase_ReturnsItAsIs()
    {
        var result = await _sut.GetDisplayCurrencyAsync("HUF");

        result.Should().Be("HUF");
    }

    [Fact]
    public async Task GetDisplayCurrency_WhenRequestedCurrencyIsWhitespace_FallsBackToUserDefault()
    {
        _db.Users.Add(new User { Id = UserId, UserName = UserId, DefaultCurrency = "GBP" });
        await _db.SaveChangesAsync();

        var result = await _sut.GetDisplayCurrencyAsync("   ");

        result.Should().Be("GBP");
    }

    [Fact]
    public async Task GetDisplayCurrency_WhenRequestedCurrencyIsNull_FallsBackToUserDefault()
    {
        _db.Users.Add(new User { Id = UserId, UserName = UserId, DefaultCurrency = "JPY" });
        await _db.SaveChangesAsync();

        var result = await _sut.GetDisplayCurrencyAsync(null);

        result.Should().Be("JPY");
    }

    [Fact]
    public async Task GetDisplayCurrency_WhenNoRequestedCurrencyAndNoUserInDb_ReturnsUsd()
    {
        var result = await _sut.GetDisplayCurrencyAsync(null);

        result.Should().Be("USD");
    }

    [Fact]
    public async Task GetDisplayCurrency_WhenUserHasDefaultCurrency_DoesNotCallRequestedCurrencyPath()
    {
        _db.Users.Add(new User { Id = UserId, UserName = UserId, DefaultCurrency = "CHF" });
        await _db.SaveChangesAsync();

        var result = await _sut.GetDisplayCurrencyAsync(null);

        // GetCurrentUserId must have been called (no requested currency → DB lookup)
        _currentUserService.Received(1).GetCurrentUserId();
        result.Should().Be("CHF");
    }

    [Fact]
    public async Task GetDisplayCurrency_WhenRequestedCurrencyProvided_DoesNotQueryDatabase()
    {
        var result = await _sut.GetDisplayCurrencyAsync("USD");

        // Should short-circuit before calling GetCurrentUserId
        _currentUserService.DidNotReceive().GetCurrentUserId();
    }

    public void Dispose() => _db.Dispose();
}

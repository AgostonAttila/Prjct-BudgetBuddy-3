using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Features.Accounts.GetAccounts;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Accounts;

public class GetAccountsHandlerTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetAccountsHandler _handler;

    public GetAccountsHandlerTests()
    {
        _handler = new GetAccountsHandler(
            _db,
            _currentUserService,
            NullLogger<GetAccountsHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenUserHasAccounts_ReturnsAccountList()
    {
        var userId = Guid.NewGuid().ToString();
        _currentUserService.GetCurrentUserId().Returns(userId);

        _db.Accounts.Add(new Account
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = "Checking",
            Description = "Main account",
            DefaultCurrencyCode = "USD",
            InitialBalance = 1000m
        });
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetAccountsQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Checking");
        result[0].DefaultCurrencyCode.Should().Be("USD");
    }

    [Fact]
    public async Task Handle_WhenUserHasNoAccounts_ReturnsEmptyList()
    {
        _currentUserService.GetCurrentUserId().Returns(Guid.NewGuid().ToString());

        var result = await _handler.Handle(new GetAccountsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsOnlyCurrentUserAccounts()
    {
        var currentUserId = Guid.NewGuid().ToString();
        var otherUserId = Guid.NewGuid().ToString();
        _currentUserService.GetCurrentUserId().Returns(currentUserId);

        _db.Accounts.AddRange(
            new Account { Id = Guid.NewGuid(), UserId = currentUserId, Name = "Mine", DefaultCurrencyCode = "HUF" },
            new Account { Id = Guid.NewGuid(), UserId = otherUserId, Name = "Theirs", DefaultCurrencyCode = "EUR" }
        );
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetAccountsQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Mine");
    }

    public void Dispose() => _db.Dispose();
}

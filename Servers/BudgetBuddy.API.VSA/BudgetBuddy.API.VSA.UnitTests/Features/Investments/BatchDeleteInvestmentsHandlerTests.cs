using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Common.Shared.Services;
using BudgetBuddy.API.VSA.Features.Investments.BatchDeleteInvestments;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Investments;

public class BatchDeleteInvestmentsHandlerTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUserCacheInvalidator _cacheInvalidator = Substitute.For<IUserCacheInvalidator>();
    private readonly BatchDeleteInvestmentsHandler _handler;
    private const string UserId = "user-123";

    public BatchDeleteInvestmentsHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        var batchDeleteService = new BatchDeleteService(NullLogger<BatchDeleteService>.Instance);
        _handler = new BatchDeleteInvestmentsHandler(_db, _currentUserService, batchDeleteService, _cacheInvalidator);
    }

    [Fact]
    public async Task Handle_WhenNoneExist_ReturnsAllAsFailed()
    {
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var command = new BatchDeleteInvestmentsCommand(ids);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.TotalRequested.Should().Be(2);
        result.SuccessCount.Should().Be(0);
        result.FailedCount.Should().Be(2);
        result.Errors.Should().HaveCount(2);
    }

    [Fact(Skip = "ExecuteDeleteAsync requires a real SQL provider — covered by integration tests")]
    public async Task Handle_WhenSomeNotFound_ReportsErrors()
    {
        var existingId = Guid.NewGuid();
        var missingId = Guid.NewGuid();

        _db.Investments.Add(new Investment { Id = existingId, UserId = UserId, Symbol = "AAPL", Name = "Apple", Type = InvestmentType.Stock, Quantity = 10, PurchasePrice = 150, CurrencyCode = "USD", PurchaseDate = new LocalDate(2024, 1, 1) });
        await _db.SaveChangesAsync();

        var command = new BatchDeleteInvestmentsCommand(new List<Guid> { existingId, missingId });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.TotalRequested.Should().Be(2);
        result.Errors.Should().HaveCount(1);
        result.Errors.Single().Should().Contain(missingId.ToString());
    }

    public void Dispose() => _db.Dispose();
}

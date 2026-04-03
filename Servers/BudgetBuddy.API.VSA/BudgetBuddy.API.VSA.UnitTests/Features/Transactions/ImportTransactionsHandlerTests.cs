using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Features.Transactions.ImportTransactions;
using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.DataExchange;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Transactions;

public class ImportTransactionsHandlerTests
{
    private readonly IDataImportService _importService = Substitute.For<IDataImportService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly ImportTransactionsHandler _handler;
    private const string UserId = "user-123";

    public ImportTransactionsHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _handler = new ImportTransactionsHandler(_importService, _currentUserService, NullLogger<ImportTransactionsHandler>.Instance);
    }

    [Fact]
    public async Task Handle_CallsImportServiceWithCorrectUserId()
    {
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        _importService.ImportTransactionsAsync(Arg.Any<Stream>(), UserId, Arg.Any<CancellationToken>())
            .Returns(new ImportResult(1, 1, 0, [], []));

        await _handler.Handle(new ImportTransactionsCommand(stream), CancellationToken.None);

        await _importService.Received(1).ImportTransactionsAsync(Arg.Any<Stream>(), UserId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsImportResultFromService()
    {
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var expected = new ImportResult(12, 10, 2, ["Row 11: invalid", "Row 12: missing field"], []);
        _importService.ImportTransactionsAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _handler.Handle(new ImportTransactionsCommand(stream), CancellationToken.None);

        result.SuccessCount.Should().Be(10);
        result.ErrorCount.Should().Be(2);
    }
}

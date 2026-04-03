using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Domain.Enums;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.DataExchange;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Features.Transactions.ExportTransactions;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Transactions;

public class ExportTransactionsHandlerTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IExportFactory _exportFactory = Substitute.For<IExportFactory>();
    private readonly ExportTransactionsHandler _handler;
    private const string UserId = "user-123";

    public ExportTransactionsHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _exportFactory.Export(ExportFormat.Csv, Arg.Any<IEnumerable<Transaction>>(), Arg.Any<Dictionary<string, Func<Transaction, object>>>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new ExportResult(new byte[] { 1, 2, 3 }, "transactions.csv", "text/csv"));
        _exportFactory.Export(ExportFormat.Excel, Arg.Any<IEnumerable<Transaction>>(), Arg.Any<Dictionary<string, Func<Transaction, object>>>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new ExportResult(new byte[] { 1, 2, 3 }, "transactions.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
        _handler = new ExportTransactionsHandler(_db, _currentUserService, _exportFactory);
    }

    [Fact]
    public async Task Handle_WhenNoTransactions_ReturnsCsvWithNoContent()
    {
        var result = await _handler.Handle(new ExportTransactionsQuery(null, null, null, null, null, null, ExportFormat.Csv), CancellationToken.None);

        result.ContentType.Should().Be("text/csv");
        result.FileContent.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WhenExcelFormat_ReturnsExcelContentType()
    {
        var result = await _handler.Handle(new ExportTransactionsQuery(null, null, null, null, null, null, ExportFormat.Excel), CancellationToken.None);

        result.ContentType.Should().Contain("spreadsheetml");
    }

    public void Dispose() => _db.Dispose();
}

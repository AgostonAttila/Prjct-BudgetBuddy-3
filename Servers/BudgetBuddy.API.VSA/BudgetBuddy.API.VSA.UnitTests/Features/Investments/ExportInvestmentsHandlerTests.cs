using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Domain.Enums;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.DataExchange;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Features.Investments.ExportInvestments;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Investments;

public class ExportInvestmentsHandlerTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IExportFactory _exportFactory = Substitute.For<IExportFactory>();
    private readonly ExportInvestmentsHandler _handler;
    private const string UserId = "user-123";

    public ExportInvestmentsHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _exportFactory.Export(ExportFormat.Csv, Arg.Any<IEnumerable<Investment>>(), Arg.Any<Dictionary<string, Func<Investment, object>>>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new ExportResult(new byte[] { 1, 2, 3 }, "investments.csv", "text/csv"));
        _exportFactory.Export(ExportFormat.Excel, Arg.Any<IEnumerable<Investment>>(), Arg.Any<Dictionary<string, Func<Investment, object>>>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new ExportResult(new byte[] { 4, 5, 6 }, "investments.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
        _handler = new ExportInvestmentsHandler(_db, _currentUserService, _exportFactory);
    }

    [Fact]
    public async Task Handle_WhenCsvFormat_ReturnsCsvContentType()
    {
        var result = await _handler.Handle(new ExportInvestmentsQuery(null, null, ExportFormat.Csv), CancellationToken.None);

        result.ContentType.Should().Be("text/csv");
    }

    [Fact]
    public async Task Handle_WhenExcelFormat_ReturnsExcelContentType()
    {
        var result = await _handler.Handle(new ExportInvestmentsQuery(null, null, ExportFormat.Excel), CancellationToken.None);

        result.ContentType.Should().Contain("spreadsheetml");
    }

    public void Dispose() => _db.Dispose();
}

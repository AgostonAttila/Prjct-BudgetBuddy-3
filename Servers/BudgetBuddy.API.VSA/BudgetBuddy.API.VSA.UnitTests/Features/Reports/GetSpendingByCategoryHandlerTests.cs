using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Features.Reports.GetSpendingByCategory;
using BudgetBuddy.API.VSA.Features.Reports.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Reports;

public class GetSpendingByCategoryHandlerTests
{
    private readonly IReportService _reportService = Substitute.For<IReportService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetSpendingByCategoryHandler _handler;
    private const string UserId = "user-123";

    public GetSpendingByCategoryHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _handler = new GetSpendingByCategoryHandler(_reportService, _currentUserService, NullLogger<GetSpendingByCategoryHandler>.Instance);
    }

    [Fact]
    public async Task Handle_CallsReportServiceWithCorrectArgs()
    {
        _reportService
            .CalculateSpendingByCategoryAsync(UserId, null, null, null, "USD", Arg.Any<CancellationToken>())
            .Returns((500m, new List<CategorySpending>()));

        await _handler.Handle(new GetSpendingByCategoryQuery(), CancellationToken.None);

        await _reportService.Received(1).CalculateSpendingByCategoryAsync(UserId, null, null, null, "USD", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsSpendingResponse()
    {
        var categories = new List<CategorySpending>
        {
            new(Guid.NewGuid(), "Food", 200m, 5, 40m)
        };
        _reportService
            .CalculateSpendingByCategoryAsync(UserId, null, null, null, "USD", Arg.Any<CancellationToken>())
            .Returns((500m, categories));

        var result = await _handler.Handle(new GetSpendingByCategoryQuery(), CancellationToken.None);

        result.TotalSpending.Should().Be(500m);
        result.Categories.Should().HaveCount(1);
        result.Currency.Should().Be("USD");
    }
}

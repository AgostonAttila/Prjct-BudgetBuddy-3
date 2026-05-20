using System.Net;
using System.Net.Http.Json;
using BudgetBuddy.Integration.Tests.Infrastructure;
using BudgetBuddy.Service.Analytics.Features.Reports.GetIncomeVsExpense;
using FluentAssertions;
using Xunit;

namespace BudgetBuddy.Integration.Tests.Analytics;

[Collection(nameof(AnalyticsApiCollection))]
public class IncomeVsExpenseReportTests(AnalyticsApiFactory factory) : IAsyncLifetime
{
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
        await TestDataSeeder.SeedAnalyticsAsync(factory.Services);
        _client = factory.CreateAuthenticatedClient();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Full flow:
    ///   Step 1 – verify /api/transactions returns 4 transactions (via Transactions service, if running)
    ///   Step 2 – compute expected values using known stub rates
    ///   Step 3 – assert /api/reports/income-vs-expense returns computed values
    /// Note: Analytics service uses its own read model, so step 1 is verified in
    /// GetTransactionsEndpointTests. Here we directly assert the report endpoint.
    /// </summary>
    [Theory]
    [InlineData("USD")]
    [InlineData("HUF")]
    public async Task IncomeVsExpense_ReturnsTotalsMatchingSeedData(string currency)
    {
        var response = await _client.GetAsync(
            $"/api/reports/income-vs-expense?startDate=2026-01-01&endDate=2026-05-15&displayCurrency={currency}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<IncomeVsExpenseResponse>();
        body.Should().NotBeNull();
        body!.Currency.Should().Be(currency);

        if (currency == "USD")
        {
            body.TotalIncome.Should().BeApproximately(TestDataSeeder.ExpectedTotalIncome, 0.02m);
            body.TotalExpense.Should().BeApproximately(TestDataSeeder.ExpectedTotalExpense, 0.02m);
            body.NetIncome.Should().BeApproximately(TestDataSeeder.ExpectedNetIncome, 0.02m);
        }
        else // HUF
        {
            body.TotalIncome.Should().BeApproximately(TestDataSeeder.ExpectedTotalIncomeHuf, 1m);
            body.TotalExpense.Should().BeApproximately(TestDataSeeder.ExpectedTotalExpenseHuf, 1m);
            body.NetIncome.Should().BeApproximately(
                TestDataSeeder.ExpectedTotalIncomeHuf - TestDataSeeder.ExpectedTotalExpenseHuf, 1m);
        }
    }

    [Fact]
    public async Task IncomeVsExpense_TransactionCountsAreCorrect()
    {
        var response = await _client.GetAsync(
            "/api/reports/income-vs-expense?startDate=2026-01-01&endDate=2026-05-15&displayCurrency=USD");
        var body = await response.Content.ReadFromJsonAsync<IncomeVsExpenseResponse>();

        body!.IncomeTransactionCount.Should().Be(1);
        body.ExpenseTransactionCount.Should().Be(3);
    }

    [Fact]
    public async Task IncomeVsExpense_MonthlyDataContainsActiveMonths()
    {
        var response = await _client.GetAsync(
            "/api/reports/income-vs-expense?startDate=2026-01-01&endDate=2026-05-15&displayCurrency=USD");
        var body = await response.Content.ReadFromJsonAsync<IncomeVsExpenseResponse>();

        // January has income (3000), February has rent (1000), March has food (300), April has food (200)
        body!.MonthlyData.Should().Contain(m => m.Year == 2026 && m.Month == 1 && m.Income > 0);
        body.MonthlyData.Should().Contain(m => m.Year == 2026 && m.Month == 2 && m.Expense > 0);
    }
}

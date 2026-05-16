using System.Net;
using System.Net.Http.Json;
using BudgetBuddy.Integration.Tests.Infrastructure;
using BudgetBuddy.Service.Analytics.Features.Reports.GetSpendingByCategory;
using FluentAssertions;
using Xunit;

namespace BudgetBuddy.Integration.Tests.Analytics;

[Collection(nameof(AnalyticsApiCollection))]
public class SpendingByCategoryReportTests(AnalyticsApiFactory factory) : IAsyncLifetime
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

    [Theory]
    [InlineData("USD")]
    [InlineData("HUF")]
    public async Task SpendingByCategory_ReturnsTwoCategories(string currency)
    {
        var response = await _client.GetAsync(
            $"/api/reports/spending-by-category?startDate=2026-01-01&endDate=2026-05-15&displayCurrency={currency}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<SpendingByCategoryResponse>();
        body.Should().NotBeNull();
        body!.Currency.Should().Be(currency);

        // Rent (1000) and Food (300+200=500)
        body.Categories.Should().HaveCount(2);
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("HUF")]
    public async Task SpendingByCategory_FoodAndRentAmountsAreCorrect(string currency)
    {
        var response = await _client.GetAsync(
            $"/api/reports/spending-by-category?startDate=2026-01-01&endDate=2026-05-15&displayCurrency={currency}");
        var body = await response.Content.ReadFromJsonAsync<SpendingByCategoryResponse>();

        var food = body!.Categories.Single(c => c.CategoryName == "Food");
        var rent = body.Categories.Single(c => c.CategoryName == "Rent");

        if (currency == "USD")
        {
            food.Amount.Should().BeApproximately(TestDataSeeder.ExpectedFoodSpending, 0.02m);
            rent.Amount.Should().BeApproximately(TestDataSeeder.RentAmount, 0.02m);
        }
        else // HUF
        {
            food.Amount.Should().BeApproximately(TestDataSeeder.ExpectedFoodSpendingHuf, 1m);
            rent.Amount.Should().BeApproximately(TestDataSeeder.RentAmount * TestDataSeeder.HufRate, 1m);
        }
    }

    [Fact]
    public async Task SpendingByCategory_TotalMatchesSumOfCategories_Usd()
    {
        var response = await _client.GetAsync(
            "/api/reports/spending-by-category?startDate=2026-01-01&endDate=2026-05-15&displayCurrency=USD");
        var body = await response.Content.ReadFromJsonAsync<SpendingByCategoryResponse>();

        body!.TotalSpending.Should().BeApproximately(TestDataSeeder.ExpectedTotalExpense, 0.02m);
    }
}

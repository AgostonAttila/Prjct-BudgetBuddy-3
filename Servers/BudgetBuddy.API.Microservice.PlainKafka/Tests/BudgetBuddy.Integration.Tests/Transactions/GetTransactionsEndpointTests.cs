using System.Net;
using System.Net.Http.Json;
using BudgetBuddy.Integration.Tests.Infrastructure;
using BudgetBuddy.Service.Transactions.Features.GetTransactions;
using BudgetBuddy.Shared.Kernel.Enums;
using FluentAssertions;
using Xunit;

namespace BudgetBuddy.Integration.Tests.Transactions;

[Collection(nameof(TransactionsApiCollection))]
public class GetTransactionsEndpointTests(TransactionsApiFactory factory) : IAsyncLifetime
{
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
        await TestDataSeeder.SeedTransactionsAsync(factory.Services);
        _client = factory.CreateAuthenticatedClient();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Full flow: verify /api/transactions returns the 4 seeded transactions.
    /// Transactions store amounts in their native currency (USD) — no conversion.
    /// </summary>
    [Fact]
    public async Task GetTransactions_ReturnsAllTransactionsInDateRange()
    {
        var response = await _client.GetAsync(
            "/api/transactions?pageNumber=1&pageSize=50&startDate=2026-01-01&endDate=2026-12-31");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<GetTransactionsResponse>(TestJsonOptions.Default);
        body.Should().NotBeNull();
        body!.TotalCount.Should().Be(4);
    }

    [Fact]
    public async Task GetTransactions_IncomeAndExpenseCountsMatch()
    {
        var response = await _client.GetAsync(
            "/api/transactions?pageNumber=1&pageSize=50&startDate=2026-01-01&endDate=2026-12-31");
        var body = await response.Content.ReadFromJsonAsync<GetTransactionsResponse>(TestJsonOptions.Default);

        body!.Transactions.Count(t => t.TransactionType == TransactionType.Income).Should().Be(1);
        body.Transactions.Count(t => t.TransactionType == TransactionType.Expense).Should().Be(3);
    }

    [Fact]
    public async Task GetTransactions_IncomeTotalMatchesSeed()
    {
        var response = await _client.GetAsync(
            "/api/transactions?pageNumber=1&pageSize=50&startDate=2026-01-01&endDate=2026-12-31");
        var body = await response.Content.ReadFromJsonAsync<GetTransactionsResponse>(TestJsonOptions.Default);

        var totalIncome = body!.Transactions
            .Where(t => t.TransactionType == TransactionType.Income)
            .Sum(t => t.Amount);
        totalIncome.Should().Be(TestDataSeeder.ExpectedTotalIncome);
    }

    [Fact]
    public async Task GetTransactions_ExpenseTotalMatchesSeed()
    {
        var response = await _client.GetAsync(
            "/api/transactions?pageNumber=1&pageSize=50&startDate=2026-01-01&endDate=2026-12-31");
        var body = await response.Content.ReadFromJsonAsync<GetTransactionsResponse>(TestJsonOptions.Default);

        var totalExpense = body!.Transactions
            .Where(t => t.TransactionType == TransactionType.Expense)
            .Sum(t => t.Amount);
        totalExpense.Should().Be(TestDataSeeder.ExpectedTotalExpense);
    }

    [Fact]
    public async Task GetTransactions_DateRangeFilter_ExcludesOutOfRange()
    {
        // Only April transactions
        var response = await _client.GetAsync(
            "/api/transactions?pageNumber=1&pageSize=50&startDate=2026-04-01&endDate=2026-04-30");
        var body = await response.Content.ReadFromJsonAsync<GetTransactionsResponse>(TestJsonOptions.Default);

        body!.TotalCount.Should().Be(1);
        body.Transactions.Single().Amount.Should().Be(TestDataSeeder.FoodApril);
    }
}

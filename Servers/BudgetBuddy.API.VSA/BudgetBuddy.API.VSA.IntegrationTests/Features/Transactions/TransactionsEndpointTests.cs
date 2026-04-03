using System.Net;
using System.Net.Http.Json;
using BudgetBuddy.API.VSA.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace BudgetBuddy.API.VSA.IntegrationTests.Features.Transactions;

[Collection("Integration")]
public class TransactionsEndpointTests(IntegrationTestWebAppFactory factory) : IAsyncLifetime
{
    private HttpClient _client = null!;
    private Guid _accountId;

    public async Task InitializeAsync()
    {
        _client = await AuthenticatedHttpClient.CreateAsync(factory,
            email: $"transactions_{Guid.NewGuid()}@test.com");

        var accountResponse = await _client.PostAsJsonAsync("/api/accounts", new
        {
            Name = "Main Account",
            Description = "Test account",
            DefaultCurrencyCode = "HUF",
            InitialBalance = 100000m
        });
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountResponse>();
        _accountId = account!.Id;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GET_Transactions_WhenNone_Returns200()
    {
        var response = await _client.GetAsync("/api/transactions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetTransactionsResponse>();
        result!.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task POST_CreateTransaction_ValidRequest_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/transactions", new
        {
            AccountId = _accountId,
            Amount = 5000m,
            CurrencyCode = "HUF",
            TransactionType = "Expense",
            PaymentType = "Cash",
            TransactionDate = "2026-03-15",
            IsTransfer = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<TransactionResponse>();
        created!.AccountId.Should().Be(_accountId);
        created.Amount.Should().Be(5000m);
        created.TransactionType.Should().Be("Expense");
    }

    [Fact]
    public async Task POST_CreateTransaction_ZeroAmount_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/transactions", new
        {
            AccountId = _accountId,
            Amount = 0m,
            CurrencyCode = "HUF",
            TransactionType = "Expense",
            PaymentType = "Cash",
            TransactionDate = "2026-03-15",
            IsTransfer = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_Transactions_AfterCreation_ReturnsCreatedTransaction()
    {
        await _client.PostAsJsonAsync("/api/transactions", new
        {
            AccountId = _accountId,
            Amount = 3000m,
            CurrencyCode = "HUF",
            TransactionType = "Income",
            PaymentType = "BankTransfer",
            TransactionDate = "2026-03-20",
            IsTransfer = false
        });

        var response = await _client.GetAsync($"/api/transactions?accountId={_accountId}");
        var result = await response.Content.ReadFromJsonAsync<GetTransactionsResponse>();

        result!.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task DELETE_Transaction_Returns204()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/transactions", new
        {
            AccountId = _accountId,
            Amount = 1000m,
            CurrencyCode = "HUF",
            TransactionType = "Expense",
            PaymentType = "Card",
            TransactionDate = "2026-03-10",
            IsTransfer = false
        });
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionResponse>();

        var deleteResponse = await _client.DeleteAsync($"/api/transactions/{created!.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DELETE_BatchTransactions_Returns200()
    {
        var t1 = await _client.PostAsJsonAsync("/api/transactions", new
        {
            AccountId = _accountId,
            Amount = 100m,
            CurrencyCode = "HUF",
            TransactionType = "Expense",
            PaymentType = "Cash",
            TransactionDate = "2026-02-01",
            IsTransfer = false
        });
        var t2 = await _client.PostAsJsonAsync("/api/transactions", new
        {
            AccountId = _accountId,
            Amount = 200m,
            CurrencyCode = "HUF",
            TransactionType = "Expense",
            PaymentType = "Cash",
            TransactionDate = "2026-02-02",
            IsTransfer = false
        });
        var tx1 = await t1.Content.ReadFromJsonAsync<TransactionResponse>();
        var tx2 = await t2.Content.ReadFromJsonAsync<TransactionResponse>();

        var request = new HttpRequestMessage(HttpMethod.Delete, "/api/transactions/batch")
        {
            Content = JsonContent.Create(new { TransactionIds = new[] { tx1!.Id, tx2!.Id } })
        };
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BatchDeleteResponse>();
        result!.SuccessCount.Should().Be(2);
    }

    [Fact]
    public async Task GET_Transactions_WithoutAuth_Returns401()
    {
        var unauthenticatedClient = factory.CreateClient();
        var response = await unauthenticatedClient.GetAsync("/api/transactions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record AccountResponse(Guid Id, string Name, string Description, string DefaultCurrencyCode, decimal InitialBalance);
    private sealed record TransactionResponse(Guid Id, Guid AccountId, decimal Amount, string CurrencyCode, string TransactionType, string PaymentType);
    private sealed record GetTransactionsResponse(int TotalCount, int PageNumber, int PageSize, List<object> Transactions);
    private sealed record BatchDeleteResponse(int TotalRequested, int SuccessCount, int FailedCount);
}

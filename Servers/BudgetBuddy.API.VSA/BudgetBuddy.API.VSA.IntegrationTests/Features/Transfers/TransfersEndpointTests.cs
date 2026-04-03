using System.Net;
using System.Net.Http.Json;
using BudgetBuddy.API.VSA.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace BudgetBuddy.API.VSA.IntegrationTests.Features.Transfers;

[Collection("Integration")]
public class TransfersEndpointTests(IntegrationTestWebAppFactory factory) : IAsyncLifetime
{
    private HttpClient _client = null!;
    private Guid _fromAccountId;
    private Guid _toAccountId;

    public async Task InitializeAsync()
    {
        _client = await AuthenticatedHttpClient.CreateAsync(factory,
            email: $"transfers_{Guid.NewGuid()}@test.com");

        var from = await _client.PostAsJsonAsync("/api/accounts", new
        {
            Name = "Checking",
            Description = "",
            DefaultCurrencyCode = "HUF",
            InitialBalance = 200000m
        });
        var fromAccount = await from.Content.ReadFromJsonAsync<AccountResponse>();
        _fromAccountId = fromAccount!.Id;

        var to = await _client.PostAsJsonAsync("/api/accounts", new
        {
            Name = "Savings",
            Description = "",
            DefaultCurrencyCode = "HUF",
            InitialBalance = 50000m
        });
        var toAccount = await to.Content.ReadFromJsonAsync<AccountResponse>();
        _toAccountId = toAccount!.Id;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task POST_CreateTransfer_ValidRequest_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/transfers", new
        {
            FromAccountId = _fromAccountId,
            ToAccountId = _toAccountId,
            Amount = 10000m,
            CurrencyCode = "HUF",
            PaymentType = "BankTransfer",
            TransferDate = "2026-03-15"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<TransferResponse>();
        created!.FromAccountId.Should().Be(_fromAccountId);
        created.ToAccountId.Should().Be(_toAccountId);
        created.Amount.Should().Be(10000m);
    }

    [Fact]
    public async Task POST_CreateTransfer_SameAccount_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/transfers", new
        {
            FromAccountId = _fromAccountId,
            ToAccountId = _fromAccountId,
            Amount = 5000m,
            CurrencyCode = "HUF",
            PaymentType = "BankTransfer",
            TransferDate = "2026-03-15"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_CreateTransfer_ZeroAmount_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/transfers", new
        {
            FromAccountId = _fromAccountId,
            ToAccountId = _toAccountId,
            Amount = 0m,
            CurrencyCode = "HUF",
            PaymentType = "BankTransfer",
            TransferDate = "2026-03-15"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_CreateTransfer_WithoutAuth_Returns401()
    {
        var unauthenticatedClient = factory.CreateClient();
        var response = await unauthenticatedClient.PostAsJsonAsync("/api/transfers", new
        {
            FromAccountId = _fromAccountId,
            ToAccountId = _toAccountId,
            Amount = 5000m,
            CurrencyCode = "HUF",
            PaymentType = "BankTransfer",
            TransferDate = "2026-03-15"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record AccountResponse(Guid Id, string Name, string Description, string DefaultCurrencyCode, decimal InitialBalance);
    private sealed record TransferResponse(Guid FromTransactionId, Guid ToTransactionId, Guid FromAccountId, Guid ToAccountId, decimal Amount, string CurrencyCode);
}

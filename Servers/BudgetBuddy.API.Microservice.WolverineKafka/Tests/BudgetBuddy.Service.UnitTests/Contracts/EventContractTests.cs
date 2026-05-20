using System.Text.Json;
using BudgetBuddy.Shared.Kernel.Enums;
using BudgetBuddy.Shared.Messages.Events.Accounts;
using BudgetBuddy.Shared.Messages.Events.Budgets;
using BudgetBuddy.Shared.Messages.Events.Transactions;
using FluentAssertions;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Xunit;

namespace BudgetBuddy.Service.UnitTests.Contracts;

/// <summary>
/// T-02: Event contract tests — verify that integration events serialize to the expected
/// JSON shape and survive a round-trip without data loss. These act as snapshot guards:
/// a failing test warns the team before a breaking schema change reaches production.
/// </summary>
public class EventContractTests
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy      = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    }.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);

    // ── AccountCreatedEvent ───────────────────────────────────────────────────

    [Fact]
    public void AccountCreatedEvent_Serializes_ExpectedProperties()
    {
        var evt = new AccountCreatedEvent
        {
            AccountId      = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            UserId         = "user-abc",
            Name           = "Savings",
            Currency       = "HUF",
            InitialBalance = 500_000m,
            CorrelationId  = Guid.Parse("22222222-2222-2222-2222-222222222222"),
        };

        var json = JsonSerializer.Serialize(evt, JsonOptions);
        using var doc = JsonDocument.Parse(json);

        doc.RootElement.GetProperty("accountId").GetGuid().Should().Be(evt.AccountId);
        doc.RootElement.GetProperty("userId").GetString().Should().Be("user-abc");
        doc.RootElement.GetProperty("name").GetString().Should().Be("Savings");
        doc.RootElement.GetProperty("currency").GetString().Should().Be("HUF");
        doc.RootElement.GetProperty("initialBalance").GetDecimal().Should().Be(500_000m);
        doc.RootElement.GetProperty("version").GetString().Should().Be("1.0");
        doc.RootElement.TryGetProperty("messageId", out _).Should().BeTrue("messageId is required for idempotency");
        doc.RootElement.TryGetProperty("correlationId", out _).Should().BeTrue("correlationId must be propagated");
        doc.RootElement.TryGetProperty("occurredAt", out _).Should().BeTrue("occurredAt is required for ordering");
    }

    [Fact]
    public void AccountCreatedEvent_RoundTrip_PreservesAllValues()
    {
        var original = new AccountCreatedEvent
        {
            AccountId      = Guid.NewGuid(),
            UserId         = "user-xyz",
            Name           = "Checking",
            Currency       = "USD",
            InitialBalance = 1_000.50m,
            CorrelationId  = Guid.NewGuid(),
        };

        var json       = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<AccountCreatedEvent>(json, JsonOptions)!;

        deserialized.AccountId.Should().Be(original.AccountId);
        deserialized.UserId.Should().Be(original.UserId);
        deserialized.Name.Should().Be(original.Name);
        deserialized.Currency.Should().Be(original.Currency);
        deserialized.InitialBalance.Should().Be(original.InitialBalance);
        deserialized.MessageId.Should().Be(original.MessageId);
        deserialized.CorrelationId.Should().Be(original.CorrelationId);
        deserialized.Version.Should().Be("1.0");
    }

    // ── TransactionCreatedEvent ───────────────────────────────────────────────

    [Fact]
    public void TransactionCreatedEvent_Serializes_ExpectedProperties()
    {
        var evt = new TransactionCreatedEvent
        {
            TransactionId   = Guid.NewGuid(),
            UserId          = "user-abc",
            AccountId       = Guid.NewGuid(),
            Amount          = 12_500m,
            CurrencyCode    = "HUF",
            TransactionType = TransactionType.Expense,
            TransactionDate = new LocalDate(2025, 4, 1),
            IsTransfer      = false,
            CorrelationId   = Guid.NewGuid(),
        };

        var json = JsonSerializer.Serialize(evt, JsonOptions);
        using var doc = JsonDocument.Parse(json);

        doc.RootElement.GetProperty("transactionId").GetGuid().Should().Be(evt.TransactionId);
        doc.RootElement.GetProperty("amount").GetDecimal().Should().Be(12_500m);
        doc.RootElement.GetProperty("currencyCode").GetString().Should().Be("HUF");
        doc.RootElement.GetProperty("isTransfer").GetBoolean().Should().BeFalse();
        doc.RootElement.TryGetProperty("transferToAccountId", out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("transactionDate", out _).Should().BeTrue();
    }

    [Fact]
    public void TransactionCreatedEvent_RoundTrip_PreservesLocalDate()
    {
        var date     = new LocalDate(2025, 12, 25);
        var original = new TransactionCreatedEvent
        {
            TransactionId   = Guid.NewGuid(),
            UserId          = "u",
            AccountId       = Guid.NewGuid(),
            Amount          = 1m,
            CurrencyCode    = "EUR",
            TransactionType = TransactionType.Income,
            TransactionDate = date,
            CorrelationId   = Guid.NewGuid(),
        };

        var json         = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<TransactionCreatedEvent>(json, JsonOptions)!;

        deserialized.TransactionDate.Should().Be(date, "NodaTime LocalDate must survive JSON round-trip");
        deserialized.TransactionType.Should().Be(TransactionType.Income);
    }

    // ── BudgetCreatedEvent ────────────────────────────────────────────────────

    [Fact]
    public void BudgetCreatedEvent_Serializes_ExpectedProperties()
    {
        var evt = new BudgetCreatedEvent
        {
            BudgetId      = Guid.NewGuid(),
            UserId        = "user-abc",
            CategoryId    = Guid.NewGuid(),
            Amount        = 200_000m,
            CurrencyCode  = "HUF",
            Year          = 2025,
            Month         = 4,
            CorrelationId = Guid.NewGuid(),
        };

        var json = JsonSerializer.Serialize(evt, JsonOptions);
        using var doc = JsonDocument.Parse(json);

        doc.RootElement.GetProperty("budgetId").GetGuid().Should().Be(evt.BudgetId);
        doc.RootElement.GetProperty("amount").GetDecimal().Should().Be(200_000m);
        doc.RootElement.GetProperty("year").GetInt32().Should().Be(2025);
        doc.RootElement.GetProperty("month").GetInt32().Should().Be(4);
        doc.RootElement.GetProperty("currencyCode").GetString().Should().Be("HUF");
        doc.RootElement.TryGetProperty("categoryId", out _).Should().BeTrue();
    }

    [Fact]
    public void BudgetCreatedEvent_RoundTrip_PreservesAllValues()
    {
        var original = new BudgetCreatedEvent
        {
            BudgetId      = Guid.NewGuid(),
            UserId        = "u2",
            CategoryId    = Guid.NewGuid(),
            Amount        = 99_999.99m,
            CurrencyCode  = "EUR",
            Year          = 2026,
            Month         = 1,
            CorrelationId = Guid.NewGuid(),
        };

        var json         = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<BudgetCreatedEvent>(json, JsonOptions)!;

        deserialized.BudgetId.Should().Be(original.BudgetId);
        deserialized.Amount.Should().Be(original.Amount);
        deserialized.Year.Should().Be(2026);
        deserialized.Month.Should().Be(1);
    }

    // ── Schema evolution guard ────────────────────────────────────────────────

    [Fact]
    public void IntegrationEvent_UnknownFields_AreIgnoredOnDeserialization()
    {
        // Simulates a newer producer adding a field that this consumer doesn't know about.
        const string json = """
            {
                "accountId": "33333333-3333-3333-3333-333333333333",
                "userId": "u",
                "name": "Test",
                "currency": "EUR",
                "initialBalance": 0,
                "messageId": "44444444-4444-4444-4444-444444444444",
                "correlationId": "55555555-5555-5555-5555-555555555555",
                "occurredAt": "2025-01-01T00:00:00Z",
                "version": "2.0",
                "newFieldFromFutureProducer": "should-be-ignored"
            }
            """;

        var act = () => JsonSerializer.Deserialize<AccountCreatedEvent>(json, JsonOptions);

        act.Should().NotThrow("unknown fields must be tolerated for forward compatibility (A-01)");
        var result = act();
        result!.Name.Should().Be("Test");
    }
}

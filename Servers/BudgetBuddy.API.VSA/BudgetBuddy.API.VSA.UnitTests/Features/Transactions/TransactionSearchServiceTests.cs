using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Features.Transactions.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Transactions;

public class TransactionSearchServiceTests
{
    private readonly TransactionSearchService _service =
        new(NullLogger<TransactionSearchService>.Instance);

    // ── ApplySearch ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!@#$")]
    public void ApplySearch_WhenSearchTermProducesNoTokens_ReturnsOriginalQuery(string searchTerm)
    {
        var query = new List<Transaction>().AsQueryable();

        var result = _service.ApplySearch(query, searchTerm);

        result.Should().BeSameAs(query);
    }

    [Fact]
    public void ApplySearch_WhenSearchTermHasTokens_ReturnsModifiedQuery()
    {
        var query = new List<Transaction>().AsQueryable();

        var result = _service.ApplySearch(query, "groceries");

        result.Should().NotBeSameAs(query);
    }
}

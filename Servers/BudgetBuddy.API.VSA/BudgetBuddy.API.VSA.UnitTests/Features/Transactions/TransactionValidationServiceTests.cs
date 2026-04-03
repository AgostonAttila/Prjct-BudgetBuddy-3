using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Features.Transactions.Services;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Transactions;

public class TransactionValidationServiceTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly TransactionValidationService _service;
    private const string UserId = "user-validation-1";

    public TransactionValidationServiceTests()
    {
        _service = new TransactionValidationService(_db, NullLogger<TransactionValidationService>.Instance);
    }

    // ── ValidateAccountOwnershipAsync ─────────────────────────────────────────

    [Fact]
    public async Task ValidateAccountOwnershipAsync_WhenAccountExistsAndBelongsToUser_DoesNotThrow()
    {
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account { Id = accountId, UserId = UserId, Name = "Test", DefaultCurrencyCode = "USD" });
        await _db.SaveChangesAsync();

        var act = () => _service.ValidateAccountOwnershipAsync(accountId, UserId);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ValidateAccountOwnershipAsync_WhenAccountNotFound_ThrowsNotFoundException()
    {
        var act = () => _service.ValidateAccountOwnershipAsync(Guid.NewGuid(), UserId);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ValidateAccountOwnershipAsync_WhenAccountBelongsToOtherUser_ThrowsNotFoundException()
    {
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account { Id = accountId, UserId = "other-user", Name = "Test", DefaultCurrencyCode = "USD" });
        await _db.SaveChangesAsync();

        var act = () => _service.ValidateAccountOwnershipAsync(accountId, UserId);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── ValidateTransferDestinationAsync ──────────────────────────────────────

    [Fact]
    public async Task ValidateTransferDestinationAsync_WhenDestinationBelongsToUser_DoesNotThrow()
    {
        var destId = Guid.NewGuid();
        _db.Accounts.Add(new Account { Id = destId, UserId = UserId, Name = "Dest", DefaultCurrencyCode = "USD" });
        await _db.SaveChangesAsync();

        var act = () => _service.ValidateTransferDestinationAsync(destId, UserId);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ValidateTransferDestinationAsync_WhenDestinationNotFound_ThrowsNotFoundException()
    {
        var act = () => _service.ValidateTransferDestinationAsync(Guid.NewGuid(), UserId);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── WarnIfDuplicateAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task WarnIfDuplicateAsync_WhenNoDuplicate_DoesNotThrow()
    {
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account { Id = accountId, UserId = UserId, Name = "Test", DefaultCurrencyCode = "USD" });
        await _db.SaveChangesAsync();

        var act = () => _service.WarnIfDuplicateAsync(accountId, 100m, new LocalDate(2024, 6, 1), null, UserId);
        await act.Should().NotThrowAsync();
    }

    public void Dispose() => _db.Dispose();
}

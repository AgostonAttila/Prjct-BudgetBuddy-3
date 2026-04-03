using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Domain.Enums;
using BudgetBuddy.API.VSA.Common.Exceptions;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Features.Transfers.Services;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Transfers;

public class TransferServiceTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly TransferService _service;
    private const string UserId = "user-123";

    public TransferServiceTests()
    {
        _service = new TransferService(_db, NullLogger<TransferService>.Instance);
    }

    [Fact]
    public async Task CreateTransferAsync_WhenFromAccountNotFound_ThrowsNotFoundException()
    {
        var act = () => _service.CreateTransferAsync(
            UserId, Guid.NewGuid(), Guid.NewGuid(), 100m, "USD", new LocalDate(2024, 6, 1), PaymentType.BankTransfer);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateTransferAsync_WhenToAccountNotFound_ThrowsNotFoundException()
    {
        var fromId = Guid.NewGuid();
        _db.Accounts.Add(new Account { Id = fromId, UserId = UserId, Name = "Source", DefaultCurrencyCode = "USD" });
        await _db.SaveChangesAsync();

        var act = () => _service.CreateTransferAsync(
            UserId, fromId, Guid.NewGuid(), 100m, "USD", new LocalDate(2024, 6, 1), PaymentType.BankTransfer);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateTransferAsync_WhenBothAccountsExist_CreatesTwoTransactions()
    {
        var fromId = Guid.NewGuid();
        var toId = Guid.NewGuid();
        _db.Accounts.AddRange(
            new Account { Id = fromId, UserId = UserId, Name = "Checking", DefaultCurrencyCode = "USD" },
            new Account { Id = toId, UserId = UserId, Name = "Savings", DefaultCurrencyCode = "USD" }
        );
        await _db.SaveChangesAsync();

        var (fromTxId, toTxId) = await _service.CreateTransferAsync(
            UserId, fromId, toId, 500m, "USD", new LocalDate(2024, 6, 15), PaymentType.BankTransfer);

        _db.Transactions.Should().HaveCount(2);
        fromTxId.Should().NotBeEmpty();
        toTxId.Should().NotBeEmpty();
        fromTxId.Should().NotBe(toTxId);
    }

    [Fact]
    public async Task CreateTransferAsync_BothTransactionsAreMarkedAsTransfer()
    {
        var fromId = Guid.NewGuid();
        var toId = Guid.NewGuid();
        _db.Accounts.AddRange(
            new Account { Id = fromId, UserId = UserId, Name = "From", DefaultCurrencyCode = "USD" },
            new Account { Id = toId, UserId = UserId, Name = "To", DefaultCurrencyCode = "USD" }
        );
        await _db.SaveChangesAsync();

        await _service.CreateTransferAsync(UserId, fromId, toId, 100m, "USD", new LocalDate(2024, 6, 1), PaymentType.BankTransfer);

        _db.Transactions.ToList().All(t => t.IsTransfer).Should().BeTrue();
        _db.Transactions.ToList().All(t => t.TransactionType == TransactionType.Transfer).Should().BeTrue();
    }

    public void Dispose() => _db.Dispose();
}

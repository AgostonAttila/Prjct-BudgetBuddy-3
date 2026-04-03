using BudgetBuddy.API.VSA.Common.Domain.Enums;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Features.Transfers.CreateTransfer;
using BudgetBuddy.API.VSA.Features.Transfers.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Transfers;

public class CreateTransferHandlerTests
{
    private readonly ITransferService _transferService = Substitute.For<ITransferService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly CreateTransferHandler _handler;
    private const string UserId = "user-123";

    public CreateTransferHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _handler = new CreateTransferHandler(_transferService, _currentUserService, NullLogger<CreateTransferHandler>.Instance);
    }

    [Fact]
    public async Task Handle_CallsTransferServiceWithCorrectArgs()
    {
        var fromId = Guid.NewGuid();
        var toId = Guid.NewGuid();
        var date = new LocalDate(2024, 6, 15);
        var fromTxId = Guid.NewGuid();
        var toTxId = Guid.NewGuid();

        _transferService
            .CreateTransferAsync(UserId, fromId, toId, 200m, "USD", date, PaymentType.BankTransfer, null, Arg.Any<CancellationToken>())
            .Returns((fromTxId, toTxId));

        var cmd = new CreateTransferCommand(fromId, toId, 200m, "USD", PaymentType.BankTransfer, null, date);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.FromTransactionId.Should().Be(fromTxId);
        result.ToTransactionId.Should().Be(toTxId);
        result.Amount.Should().Be(200m);
    }

    [Fact]
    public async Task Handle_WhenServiceThrows_PropagatesException()
    {
        _transferService.CreateTransferAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<decimal>(),
            Arg.Any<string>(), Arg.Any<LocalDate>(), Arg.Any<PaymentType>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Transfer failed"));

        var cmd = new CreateTransferCommand(Guid.NewGuid(), Guid.NewGuid(), 100m, "USD", PaymentType.BankTransfer, null, new LocalDate(2024, 6, 1));

        var act = () => _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}

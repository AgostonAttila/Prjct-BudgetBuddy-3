using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;
using BudgetBuddy.Module.Transactions.Features.Transfers.Services;

namespace BudgetBuddy.Module.Transactions.Features.Transfers.CreateTransfer;

public class CreateTransferHandler(
    ITransferService transferService,
    ICurrentUserService currentUserService,
    ILogger<CreateTransferHandler> logger) : UserAwareHandler<CreateTransferCommand, TransferResponse>(currentUserService)
{
    public override async Task<TransferResponse> Handle(
        CreateTransferCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Creating transfer from account {FromAccountId} to {ToAccountId} for user {UserId}",
            request.FromAccountId,
            request.ToAccountId,
            UserId);

        var (fromTransactionId, toTransactionId) = await transferService.CreateTransferAsync(
            UserId,
            request.FromAccountId,
            request.ToAccountId,
            request.Amount,
            request.CurrencyCode,
            request.TransferDate,
            request.PaymentType,
            request.Note,
            cancellationToken);

        logger.LogInformation(
            "Transfer created successfully. From transaction: {FromId}, To transaction: {ToId}",
            fromTransactionId,
            toTransactionId);

        return new TransferResponse(
            fromTransactionId,
            toTransactionId,
            request.FromAccountId,
            request.ToAccountId,
            request.Amount,
            request.CurrencyCode,
            request.TransferDate
        );
    }
}

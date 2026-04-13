using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Common.Repositories;
using BudgetBuddy.Application.Features.Transactions.Services;

namespace BudgetBuddy.Application.Features.Transactions.CreateTransaction;

public class CreateTransactionHandler(
    ITransactionRepository transactionRepo,
    IUnitOfWork uow,
    IMapper mapper,
    ICurrentUserService currentUserService,
    ITransactionValidationService validationService,
    IUserCacheInvalidator cacheInvalidator,
    ILogger<CreateTransactionHandler> logger) : UserAwareHandler<CreateTransactionCommand, TransactionResponse>(currentUserService)
{
    public override async Task<TransactionResponse> Handle(
        CreateTransactionCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating transaction for user {UserId}", UserId);

        await validationService.ValidateAccountOwnershipAsync(request.AccountId, UserId, cancellationToken);

        await validationService.WarnIfDuplicateAsync(
            request.AccountId, request.Amount, request.TransactionDate, request.CategoryId, UserId, cancellationToken);

        if (request is { IsTransfer: true, TransferToAccountId: not null })
            await validationService.ValidateTransferDestinationAsync(request.TransferToAccountId.Value, UserId, cancellationToken);

        var transaction = mapper.Map<Transaction>(request);
        transaction.UserId = UserId;

        await transactionRepo.AddAsync(transaction, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        await cacheInvalidator.InvalidateAsync(UserId, cancellationToken);

        logger.LogInformation("Transaction {TransactionId} created successfully", transaction.Id);

        return mapper.Map<TransactionResponse>(transaction);
    }
}

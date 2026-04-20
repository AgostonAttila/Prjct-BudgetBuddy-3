using BudgetBuddy.Module.Transactions.Features.CreateTransaction;
using BudgetBuddy.Module.Transactions.Features.Transactions.Services;
using BudgetBuddy.Shared.Contracts.Events.Transactions;
using BudgetBuddy.Shared.Infrastructure.Handlers;
using BudgetBuddy.Shared.Infrastructure.Persistence.Outbox;

namespace BudgetBuddy.Module.Transactions.Features.Transactions.CreateTransaction;

public class CreateTransactionHandler(
    TransactionsDbContext _context,
    IMapper _mapper,
    ICurrentUserService currentUserService,
    ITransactionValidationService _validationService,
    IUserCacheInvalidator _cacheInvalidator,
    IDomainEventCollector _eventCollector,
    ILogger<CreateTransactionHandler> _logger) : UserAwareHandler<CreateTransactionCommand, TransactionResponse>(currentUserService)
{
    public override async Task<TransactionResponse> Handle(
        CreateTransactionCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating transaction for user {UserId}", UserId);

        await _validationService.ValidateAccountOwnershipAsync(request.AccountId, UserId, cancellationToken);

        await _validationService.WarnIfDuplicateAsync(
            request.AccountId, request.Amount, request.TransactionDate, request.CategoryId, UserId, cancellationToken);

        if (request is { IsTransfer: true, TransferToAccountId: not null })
            await _validationService.ValidateTransferDestinationAsync(request.TransferToAccountId.Value, UserId, cancellationToken);

        var transaction = _mapper.Map<Transaction>(request);
        transaction.UserId = UserId;

        await _context.Transactions.AddAsync(transaction, cancellationToken);

        _eventCollector.Collect(new TransactionCreatedEvent(
            EventId: Guid.NewGuid(),
            TransactionId: transaction.Id,
            UserId: UserId,
            AccountId: transaction.AccountId,
            CategoryId: transaction.CategoryId,
            Amount: transaction.Amount,
            CurrencyCode: transaction.CurrencyCode,
            TransactionType: (int)transaction.TransactionType,
            TransactionDate: transaction.TransactionDate,
            IsTransfer: transaction.IsTransfer,
            TransferToAccountId: transaction.TransferToAccountId
        ));

        await _context.SaveChangesAsync(cancellationToken);
        await _cacheInvalidator.InvalidateAsync(UserId, cancellationToken);

        _logger.LogInformation("Transaction {TransactionId} created successfully", transaction.Id);

        return _mapper.Map<TransactionResponse>(transaction);
    }
}

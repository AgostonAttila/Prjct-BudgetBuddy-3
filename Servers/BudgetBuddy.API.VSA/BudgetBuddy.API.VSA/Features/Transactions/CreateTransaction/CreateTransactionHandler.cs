using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Shared.Handlers;
using BudgetBuddy.API.VSA.Common.Shared.Services;
using BudgetBuddy.API.VSA.Features.Transactions.Services;

namespace BudgetBuddy.API.VSA.Features.Transactions.CreateTransaction;

public class CreateTransactionHandler(
    AppDbContext _context,
    IMapper _mapper,
    ICurrentUserService currentUserService,
    ITransactionValidationService _validationService,
    IUserCacheInvalidator _cacheInvalidator,
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
        await _context.SaveChangesAsync(cancellationToken);
        await _cacheInvalidator.InvalidateAsync(UserId, cancellationToken);

        _logger.LogInformation("Transaction {TransactionId} created successfully", transaction.Id);

        return _mapper.Map<TransactionResponse>(transaction);
    }
}

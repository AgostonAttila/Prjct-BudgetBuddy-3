using BudgetBuddy.Service.Transactions.Features.CreateTransaction;
using BudgetBuddy.Service.Transactions.Features.Transactions.Services;
using BudgetBuddy.Shared.Infrastructure.Financial;
using BudgetBuddy.Shared.Infrastructure.Handlers;
using BudgetBuddy.Shared.Messages.Events.Transactions;
using Wolverine.EntityFrameworkCore;

namespace BudgetBuddy.Service.Transactions.Features.Transactions.CreateTransaction;

public class CreateTransactionHandler(
    TransactionsDbContext _context,
    IMapper _mapper,
    ICurrentUserService currentUserService,
    ITransactionValidationService _validationService,
    IUserCacheInvalidator _cacheInvalidator,
    IDbContextOutbox _outbox,
    ICurrencyConversionService _currencyConversionService,
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
        {
            await _validationService.ValidateTransferDestinationAsync(request.TransferToAccountId.Value, UserId, cancellationToken);
        }

        var transaction = _mapper.Map<Transaction>(request);
        transaction.UserId = UserId;

        // Capture EUR-equivalent at transaction time so budget alerts can use the historical
        // rate rather than a live rate that may have drifted (#7). Best-effort: if the FX
        // service is unavailable, RefCurrencyAmount stays null and alerts fall back to live rates.
        transaction.RefCurrencyAmount = await CaptureEurEquivalentAsync(
            transaction.Amount, transaction.CurrencyCode, cancellationToken);

        await _context.Transactions.AddAsync(transaction, cancellationToken);

        _outbox.Enroll(_context);
        await _outbox.SendAsync(new TransactionCreatedEvent
        {
            TransactionId = transaction.Id,
            UserId = UserId,
            AccountId = transaction.AccountId,
            CategoryId = transaction.CategoryId,
            Amount = transaction.Amount,
            CurrencyCode = transaction.CurrencyCode,
            TransactionType = transaction.TransactionType,
            TransactionDate = transaction.TransactionDate,
            IsTransfer = transaction.IsTransfer,
            TransferToAccountId = transaction.TransferToAccountId
        });

        await _outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
        await _cacheInvalidator.InvalidateAsync(UserId, cancellationToken);

        _logger.LogInformation("Transaction {TransactionId} created successfully", transaction.Id);

        return _mapper.Map<TransactionResponse>(transaction);
    }

    private async Task<decimal?> CaptureEurEquivalentAsync(
        decimal amount, string currencyCode, CancellationToken ct)
    {
        try
        {
            return await _currencyConversionService.ConvertAsync(amount, currencyCode, "EUR", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Could not capture historical FX rate for {Currency} → EUR; RefCurrencyAmount will be null",
                currencyCode);
            return null;
        }
    }
}

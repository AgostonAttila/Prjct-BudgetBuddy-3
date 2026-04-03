using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Shared.Handlers;
using BudgetBuddy.API.VSA.Common.Shared.Services;

namespace BudgetBuddy.API.VSA.Features.Transactions.UpdateTransaction;

public class UpdateTransactionHandler(
    AppDbContext context,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IUserCacheInvalidator cacheInvalidator,
    ILogger<UpdateTransactionHandler> logger) : UserAwareHandler<UpdateTransactionCommand, TransactionResponse>(currentUserService)
{
    public override async Task<TransactionResponse> Handle(
        UpdateTransactionCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating transaction {TransactionId} for user {UserId}", request.Id, UserId);

        var transaction = await context.Transactions
            .FirstOrDefaultAsync(t => t.Id == request.Id && t.UserId == UserId, cancellationToken);

        if (transaction == null)
            throw new NotFoundException(nameof(Transaction), request.Id);

        mapper.Map(request, transaction);

        await context.SaveChangesAsync(cancellationToken);
        await cacheInvalidator.InvalidateAsync(UserId, cancellationToken);

        logger.LogInformation("Transaction {TransactionId} updated successfully", request.Id);

        return mapper.Map<TransactionResponse>(transaction);
    }
}

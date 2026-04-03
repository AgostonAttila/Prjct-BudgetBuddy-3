using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Shared.Handlers;

namespace BudgetBuddy.API.VSA.Features.Accounts.UpdateAccount;

public class UpdateAccountHandler(
    AppDbContext context,
    IMapper mapper,
    ICurrentUserService currentUserService,
    ILogger<UpdateAccountHandler> logger) : UserAwareHandler<UpdateAccountCommand, UpdateAccountResponse>(currentUserService)
{
    public override async Task<UpdateAccountResponse> Handle(
        UpdateAccountCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating account {AccountId} for user {UserId}", request.Id, UserId);

        var account = await context.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.Id && a.UserId == UserId, cancellationToken);

        if (account == null)
            throw new NotFoundException(nameof(Account), request.Id);

        // Check for duplicate account name (excluding current account)
        // NOTE: Disabled - GlobalExceptionHandler catches DbUpdateException from unique constraint (IX_Accounts_Unique)
        // var exists = await context.Accounts
        //     .AnyAsync(a =>
        //         a.UserId == userId &&
        //         a.Name == request.Name &&
        //         a.Id != request.Id,
        //         cancellationToken);
        // if (exists)
        //     throw new ValidationException($"An account with the name '{request.Name}' already exists.");

        mapper.Map(request, account);

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Account {AccountId} updated successfully", account.Id);

        return mapper.Map<UpdateAccountResponse>(account);
    }
}

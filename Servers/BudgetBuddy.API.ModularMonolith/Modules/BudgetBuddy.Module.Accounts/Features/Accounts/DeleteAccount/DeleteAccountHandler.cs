using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;

namespace BudgetBuddy.Module.Accounts.Features.DeleteAccount;

public class DeleteAccountHandler(
    AccountsDbContext context,
    ICurrentUserService currentUserService,
    ILogger<DeleteAccountHandler> logger) : UserAwareHandler<DeleteAccountCommand, Unit>(currentUserService)
{
    public override async Task<Unit> Handle(
        DeleteAccountCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting account {AccountId} for user {UserId}", request.Id, UserId);

        var account = await context.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.Id && a.UserId == UserId, cancellationToken);

        if (account == null)
            throw new NotFoundException(nameof(Account), request.Id);

        context.Accounts.Remove(account);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Account {AccountId} deleted successfully", request.Id);

        return Unit.Value;
    }
}

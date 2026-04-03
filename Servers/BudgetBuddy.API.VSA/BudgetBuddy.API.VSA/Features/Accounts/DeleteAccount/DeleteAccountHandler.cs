using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Shared.Handlers;

namespace BudgetBuddy.API.VSA.Features.Accounts.DeleteAccount;

public class DeleteAccountHandler(
    AppDbContext context,
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

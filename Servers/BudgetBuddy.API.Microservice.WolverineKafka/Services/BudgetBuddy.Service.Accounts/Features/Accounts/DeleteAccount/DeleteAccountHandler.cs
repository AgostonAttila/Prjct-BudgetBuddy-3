using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;
using BudgetBuddy.Shared.Messages.Events.Accounts;
using Wolverine.EntityFrameworkCore;

namespace BudgetBuddy.Service.Accounts.Features.DeleteAccount;

public class DeleteAccountHandler(
    AccountsDbContext context,
    ICurrentUserService currentUserService,
    IDbContextOutbox outbox,
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
        {
            throw new NotFoundException(nameof(Account), request.Id);
        }

        context.Accounts.Remove(account);

        outbox.Enroll(context);
        await outbox.SendAsync(new AccountDeletedEvent
        {
            AccountId = account.Id,
            UserId    = UserId
        });
        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);

        logger.LogInformation("Account {AccountId} deleted successfully", request.Id);

        return Unit.Value;
    }
}

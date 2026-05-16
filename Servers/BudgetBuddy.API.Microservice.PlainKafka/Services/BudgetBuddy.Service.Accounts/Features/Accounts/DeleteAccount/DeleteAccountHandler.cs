using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Infrastructure.Persistence.Outbox;
using BudgetBuddy.Shared.Messages.Events.Accounts;
using BudgetBuddy.Shared.Messages.Topics;

namespace BudgetBuddy.Service.Accounts.Features.DeleteAccount;

public class DeleteAccountHandler(
    AccountsDbContext context,
    ICurrentUserService currentUserService,
    IEventPublisher publisher,
    IDomainEventCollector collector,
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

        collector.Collect(new AccountDeletedEvent
        {
            AccountId = account.Id,
            UserId    = UserId
        });

        context.Accounts.Remove(account);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Account {AccountId} deleted successfully", request.Id);

        await publisher.PublishTombstoneAsync(TopicNames.AccountsChangelog, account.Id.ToString(), cancellationToken);

        return Unit.Value;
    }
}

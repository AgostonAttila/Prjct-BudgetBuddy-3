using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Infrastructure.Persistence.Outbox;
using BudgetBuddy.Shared.Messages.Events.Accounts;
using BudgetBuddy.Shared.Messages.Messages;
using BudgetBuddy.Shared.Messages.Topics;

namespace BudgetBuddy.Service.Accounts.Features.UpdateAccount;

public class UpdateAccountHandler(
    AccountsDbContext context,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IEventPublisher publisher,
    IDomainEventCollector collector,
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
        {
            throw new NotFoundException(nameof(Account), request.Id);
        }

        mapper.Map(request, account);

        collector.Collect(new AccountUpdatedEvent
        {
            AccountId = account.Id,
            UserId    = UserId,
            Name      = account.Name,
            Currency  = account.DefaultCurrencyCode,
            IsActive  = true
        });

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Account {AccountId} updated successfully", account.Id);

        var snapshot = new AccountSnapshotMessage
        {
            AccountId = account.Id,
            UserId    = UserId,
            Name      = account.Name,
            Currency  = account.DefaultCurrencyCode,
            IsActive  = true,
            Balance   = account.InitialBalance
        };

        await publisher.PublishStateAsync(TopicNames.AccountsChangelog, account.Id.ToString(), snapshot, cancellationToken);

        return mapper.Map<UpdateAccountResponse>(account);
    }
}

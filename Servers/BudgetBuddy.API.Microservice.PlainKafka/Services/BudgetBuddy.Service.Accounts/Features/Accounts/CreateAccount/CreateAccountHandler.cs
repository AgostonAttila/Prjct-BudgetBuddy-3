using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Infrastructure.Persistence.Outbox;
using BudgetBuddy.Shared.Messages.Events.Accounts;
using BudgetBuddy.Shared.Messages.Messages;
using BudgetBuddy.Shared.Messages.Topics;

namespace BudgetBuddy.Service.Accounts.Features.CreateAccount;

public class CreateAccountHandler(
    AccountsDbContext context,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IEventPublisher publisher,
    IDomainEventCollector collector,
    ILogger<CreateAccountHandler> logger) : UserAwareHandler<CreateAccountCommand, CreateAccountResponse>(currentUserService)
{
    public override async Task<CreateAccountResponse> Handle(
        CreateAccountCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating account {AccountName} for user {UserId}", request.Name, UserId);

        var account = mapper.Map<Account>(request);
        account.UserId = UserId;

        collector.Collect(new AccountCreatedEvent
        {
            AccountId      = account.Id,
            UserId         = UserId,
            Name           = account.Name,
            Currency       = account.DefaultCurrencyCode,
            InitialBalance = account.InitialBalance
        });

        context.Accounts.Add(account);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Account {AccountId} created successfully", account.Id);

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

        return mapper.Map<CreateAccountResponse>(account);
    }
}

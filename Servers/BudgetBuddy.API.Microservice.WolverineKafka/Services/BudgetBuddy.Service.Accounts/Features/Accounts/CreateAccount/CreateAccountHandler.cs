using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;
using BudgetBuddy.Shared.Messages.Events.Accounts;
using Wolverine.EntityFrameworkCore;

namespace BudgetBuddy.Service.Accounts.Features.CreateAccount;

public class CreateAccountHandler(
    AccountsDbContext context,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IDbContextOutbox outbox,
    ILogger<CreateAccountHandler> logger) : UserAwareHandler<CreateAccountCommand, CreateAccountResponse>(currentUserService)
{
    public override async Task<CreateAccountResponse> Handle(
        CreateAccountCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating account {AccountName} for user {UserId}", request.Name, UserId);

        var account = mapper.Map<Account>(request);
        account.UserId = UserId;

        context.Accounts.Add(account);

        outbox.Enroll(context);
        await outbox.SendAsync(new AccountCreatedEvent
        {
            AccountId      = account.Id,
            UserId         = UserId,
            Name           = account.Name,
            Currency       = account.DefaultCurrencyCode,
            InitialBalance = account.InitialBalance
        });
        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);

        logger.LogInformation("Account {AccountId} created successfully", account.Id);

        return mapper.Map<CreateAccountResponse>(account);
    }
}

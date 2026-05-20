using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;
using BudgetBuddy.Shared.Messages.Events.Accounts;
using Wolverine.EntityFrameworkCore;

namespace BudgetBuddy.Service.Accounts.Features.UpdateAccount;

public class UpdateAccountHandler(
    AccountsDbContext context,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IDbContextOutbox outbox,
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

        outbox.Enroll(context);
        await outbox.SendAsync(new AccountUpdatedEvent
        {
            AccountId = account.Id,
            UserId    = UserId,
            Name      = account.Name,
            Currency  = account.DefaultCurrencyCode,
            IsActive  = true
        });
        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);

        logger.LogInformation("Account {AccountId} updated successfully", account.Id);

        return mapper.Map<UpdateAccountResponse>(account);
    }
}

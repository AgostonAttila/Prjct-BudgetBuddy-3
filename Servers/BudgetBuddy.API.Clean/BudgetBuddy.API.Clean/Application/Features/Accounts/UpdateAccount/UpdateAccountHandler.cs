using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Application.Features.Accounts.UpdateAccount;

public class UpdateAccountHandler(
    IAccountRepository accountRepo,
    IUnitOfWork uow,
    IMapper mapper,
    ICurrentUserService currentUserService,
    ILogger<UpdateAccountHandler> logger) : UserAwareHandler<UpdateAccountCommand, UpdateAccountResponse>(currentUserService)
{
    public override async Task<UpdateAccountResponse> Handle(
        UpdateAccountCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating account {AccountId} for user {UserId}", request.Id, UserId);

        var account = await accountRepo.GetByIdAsync(request.Id, UserId, cancellationToken);

        if (account == null)
            throw new NotFoundException(nameof(Account), request.Id);

        mapper.Map(request, account);

        await uow.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Account {AccountId} updated successfully", account.Id);

        return mapper.Map<UpdateAccountResponse>(account);
    }
}

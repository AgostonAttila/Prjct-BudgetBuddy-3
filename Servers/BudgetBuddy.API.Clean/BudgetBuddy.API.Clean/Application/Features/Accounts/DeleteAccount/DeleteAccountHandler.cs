using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Application.Features.Accounts.DeleteAccount;

public class DeleteAccountHandler(
    IAccountRepository accountRepo,
    IUnitOfWork uow,
    ICurrentUserService currentUserService,
    ILogger<DeleteAccountHandler> logger) : UserAwareHandler<DeleteAccountCommand, Unit>(currentUserService)
{
    public override async Task<Unit> Handle(
        DeleteAccountCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting account {AccountId} for user {UserId}", request.Id, UserId);

        var account = await accountRepo.GetByIdAsync(request.Id, UserId, cancellationToken);

        if (account == null)
            throw new NotFoundException(nameof(Account), request.Id);

        accountRepo.Remove(account);
        await uow.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Account {AccountId} deleted successfully", request.Id);

        return Unit.Value;
    }
}

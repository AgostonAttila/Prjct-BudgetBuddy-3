using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Application.Features.Accounts.CreateAccount;

public class CreateAccountHandler(
    IAccountRepository accountRepo,
    IUnitOfWork uow,
    IMapper mapper,
    ICurrentUserService currentUserService,
    ILogger<CreateAccountHandler> logger) : UserAwareHandler<CreateAccountCommand, CreateAccountResponse>(currentUserService)
{
    public override async Task<CreateAccountResponse> Handle(
        CreateAccountCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating account {AccountName} for user {UserId}", request.Name, UserId);

        var account = mapper.Map<Account>(request);
        account.UserId = UserId;

        accountRepo.Add(account);
        await uow.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Account {AccountId} created successfully", account.Id);

        return mapper.Map<CreateAccountResponse>(account);
    }
}

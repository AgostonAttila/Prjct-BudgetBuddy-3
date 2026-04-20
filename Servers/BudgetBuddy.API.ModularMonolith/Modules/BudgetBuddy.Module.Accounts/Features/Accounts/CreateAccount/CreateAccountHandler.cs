using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;

namespace BudgetBuddy.Module.Accounts.Features.CreateAccount;

public class CreateAccountHandler(
    AccountsDbContext context,
    IMapper mapper,
    ICurrentUserService currentUserService,
    ILogger<CreateAccountHandler> logger) : UserAwareHandler<CreateAccountCommand, CreateAccountResponse>(currentUserService)
{
    public override async Task<CreateAccountResponse> Handle(
        CreateAccountCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating account {AccountName} for user {UserId}", request.Name, UserId);

        // Check for duplicate account name
        // NOTE: Disabled - GlobalExceptionHandler catches DbUpdateException from unique constraint (IX_Accounts_Unique)
        // var exists = await context.Accounts
        //     .AnyAsync(a => a.UserId == userId && a.Name == request.Name, cancellationToken);
        // if (exists)
        //     throw new ValidationException($"An account with the name '{request.Name}' already exists.");

        var account = mapper.Map<Account>(request);
        account.UserId = UserId;

        context.Accounts.Add(account);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Account {AccountId} created successfully", account.Id);

        return mapper.Map<CreateAccountResponse>(account);
    }
}

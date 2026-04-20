using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;

namespace BudgetBuddy.Module.Accounts.Features.GetAccounts;

public class GetAccountsHandler(
    AccountsDbContext context,
    ICurrentUserService currentUserService,
    ILogger<GetAccountsHandler> logger) : UserAwareHandler<GetAccountsQuery, List<AccountDto>>(currentUserService)
{

    public override async Task<List<AccountDto>> Handle(
        GetAccountsQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching accounts for user {UserId}", UserId);


        var accountDtos = await context.Accounts
            .AsNoTracking()
            .Where(a => a.UserId == UserId)
            .Select(a => new AccountDto(
                a.Id,
                a.Name,
                a.Description,
                a.DefaultCurrencyCode,
                a.InitialBalance,
                0 // Transaction count removed — cross-module navigation no longer available
            ))
            .ToListAsync(cancellationToken);

        logger.LogInformation("Found {Count} accounts for user {UserId}", accountDtos.Count, UserId);

        return accountDtos;
    }
}

using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Shared.Handlers;

namespace BudgetBuddy.API.VSA.Features.Accounts.GetAccounts;

public class GetAccountsHandler(
    AppDbContext context,
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
                a.Transactions.Count()  
            ))
            .ToListAsync(cancellationToken);

        logger.LogInformation("Found {Count} accounts for user {UserId}", accountDtos.Count, UserId);

        return accountDtos;
    }
}

using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;

namespace BudgetBuddy.Service.Accounts.Features.GetAccounts;

public class GetAccountsHandler(
    AccountsDbContext context,
    ICurrentUserService currentUserService,
    ILogger<GetAccountsHandler> logger) : UserAwareHandler<GetAccountsQuery, List<AccountDto>>(currentUserService)
{
    private static readonly Func<AccountsDbContext, string, IAsyncEnumerable<AccountDto>> GetByUserQuery =
        EF.CompileAsyncQuery((AccountsDbContext ctx, string userId) =>
            ctx.Accounts
                .AsNoTracking()
                .Where(a => a.UserId == userId)
                .Select(a => new AccountDto(
                    a.Id,
                    a.Name,
                    a.Description,
                    a.DefaultCurrencyCode,
                    a.InitialBalance,
                    0 // Transaction count removed — cross-module navigation no longer available
                )));

    public override async Task<List<AccountDto>> Handle(
        GetAccountsQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching accounts for user {UserId}", UserId);

        var accountDtos = new List<AccountDto>();
        await foreach (var dto in GetByUserQuery(context, UserId).WithCancellation(cancellationToken))
        {
            accountDtos.Add(dto);
        }

        logger.LogInformation("Found {Count} accounts for user {UserId}", accountDtos.Count, UserId);

        return accountDtos;
    }
}

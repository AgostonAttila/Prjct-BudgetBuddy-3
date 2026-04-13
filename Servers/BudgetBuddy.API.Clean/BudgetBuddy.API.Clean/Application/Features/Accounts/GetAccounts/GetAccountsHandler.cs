using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Application.Features.Accounts.GetAccounts;

public class GetAccountsHandler(
    IAccountRepository accountRepo,
    ICurrentUserService currentUserService,
    ILogger<GetAccountsHandler> logger) : UserAwareHandler<GetAccountsQuery, List<AccountDto>>(currentUserService)
{
    public override async Task<List<AccountDto>> Handle(
        GetAccountsQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching accounts for user {UserId}", UserId);

        var summaries = await accountRepo.GetSummariesAsync(UserId, cancellationToken);

        var accountDtos = summaries
            .Select(s => new AccountDto(s.Id, s.Name, s.Description, s.DefaultCurrencyCode, s.InitialBalance, s.TransactionCount))
            .ToList();

        logger.LogInformation("Found {Count} accounts for user {UserId}", accountDtos.Count, UserId);

        return accountDtos;
    }
}

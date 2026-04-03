using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Shared.Handlers;

namespace BudgetBuddy.API.VSA.Features.Accounts.GetAccountBalance;

public class GetAccountBalanceHandler(
    AppDbContext context,
    ICurrentUserService currentUserService,
    ILogger<GetAccountBalanceHandler> logger) : UserAwareHandler<GetAccountBalanceQuery, AccountBalanceResponse>(currentUserService)
{
    public override async Task<AccountBalanceResponse> Handle(
        GetAccountBalanceQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Calculating balance for account {AccountId}", request.AccountId);

        // Load account info
        var account = await context.Accounts
            .AsNoTracking()
            .Where(a => a.Id == request.AccountId && a.UserId == UserId)
            .Select(a => new
            {
                a.Id,
                a.Name,
                a.InitialBalance
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (account == null)
            throw new NotFoundException(nameof(Account), request.AccountId);


        // Optimized: Group by TransactionType instead of dummy grouping
        var transactionsByType = await context.Transactions
            .AsNoTracking()
            .Where(t => t.AccountId == request.AccountId)
            .GroupBy(t => t.TransactionType)
            .Select(g => new
            {
                TransactionType = g.Key,
                Total = g.Sum(t => t.Amount),
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);

        // Extract totals from grouped data
        var totalIncome = transactionsByType
            .FirstOrDefault(x => x.TransactionType == TransactionType.Income)?.Total ?? 0;
        var totalExpense = transactionsByType
            .FirstOrDefault(x => x.TransactionType == TransactionType.Expense)?.Total ?? 0;
        var transactionCount = transactionsByType.Sum(x => x.Count);

        var currentBalance = account.InitialBalance + totalIncome - totalExpense;

        logger.LogInformation(
            "Account {AccountId} balance calculated: Initial={Initial}, Income={Income}, Expense={Expense}, Current={Current}",
            request.AccountId,
            account.InitialBalance,
            totalIncome,
            totalExpense,
            currentBalance);

        return new AccountBalanceResponse(
            account.Id,
            account.Name,
            account.InitialBalance,
            totalIncome,
            totalExpense,
            currentBalance,
            transactionCount
        );
    }

}



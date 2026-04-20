using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;
using BudgetBuddy.Shared.Contracts.Accounts;

namespace BudgetBuddy.Module.Accounts.Features.GetAccountBalance;

public class GetAccountBalanceHandler(
    AccountsDbContext context,
    IAccountTransactionSummary transactionSummary,
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

        // Query transaction aggregates via cross-module contract (Transactions module implements this)
        var aggregate = await transactionSummary.GetAggregateForAccountAsync(request.AccountId, cancellationToken);

        var totalIncome = aggregate.TotalIncome;
        var totalExpense = aggregate.TotalExpense;
        var transactionCount = aggregate.TransactionCount;

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



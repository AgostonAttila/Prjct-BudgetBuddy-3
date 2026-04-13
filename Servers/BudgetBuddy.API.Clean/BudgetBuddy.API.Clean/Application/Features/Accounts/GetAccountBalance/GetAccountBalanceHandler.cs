using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Application.Features.Accounts.GetAccountBalance;

public class GetAccountBalanceHandler(
    IAccountRepository accountRepo,
    ICurrentUserService currentUserService,
    ILogger<GetAccountBalanceHandler> logger) : UserAwareHandler<GetAccountBalanceQuery, AccountBalanceResponse>(currentUserService)
{
    public override async Task<AccountBalanceResponse> Handle(
        GetAccountBalanceQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Calculating balance for account {AccountId}", request.AccountId);

        var balance = await accountRepo.GetBalanceAsync(request.AccountId, UserId, cancellationToken);

        if (balance == null)
            throw new NotFoundException(nameof(Account), request.AccountId);

        var currentBalance = balance.InitialBalance + balance.TotalIncome - balance.TotalExpense;

        logger.LogInformation(
            "Account {AccountId} balance calculated: Initial={Initial}, Income={Income}, Expense={Expense}, Current={Current}",
            request.AccountId,
            balance.InitialBalance,
            balance.TotalIncome,
            balance.TotalExpense,
            currentBalance);

        return new AccountBalanceResponse(
            balance.Id,
            balance.Name,
            balance.InitialBalance,
            balance.TotalIncome,
            balance.TotalExpense,
            currentBalance,
            balance.TransactionCount
        );
    }
}

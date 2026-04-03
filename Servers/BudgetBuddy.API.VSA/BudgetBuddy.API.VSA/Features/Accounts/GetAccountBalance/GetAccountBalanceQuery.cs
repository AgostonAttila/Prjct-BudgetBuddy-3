

namespace BudgetBuddy.API.VSA.Features.Accounts.GetAccountBalance;

public record GetAccountBalanceQuery(
    Guid AccountId
) : IRequest<AccountBalanceResponse>;

public record AccountBalanceResponse(
    Guid AccountId,
    string AccountName,
    decimal InitialBalance,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal CurrentBalance,
    int TransactionCount
);

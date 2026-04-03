

namespace BudgetBuddy.API.VSA.Features.Accounts.GetAccounts;

public record GetAccountsQuery() : IRequest<List<AccountDto>>;

public record AccountDto(
    Guid Id,
    string Name,
    string Description,
    string DefaultCurrencyCode,
    decimal InitialBalance,
    int TransactionCount
);

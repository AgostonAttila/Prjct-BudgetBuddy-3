namespace BudgetBuddy.Application.Common.Repositories;

public record AccountSummary(
    Guid Id,
    string Name,
    string Description,
    string DefaultCurrencyCode,
    decimal InitialBalance,
    int TransactionCount);

public record AccountBalance(
    Guid Id,
    string Name,
    decimal InitialBalance,
    decimal TotalIncome,
    decimal TotalExpense,
    int TransactionCount);

public record AccountBalanceData(Guid Id, string Name, decimal InitialBalance, string CurrencyCode);

public interface IAccountRepository : IUserOwnedRepository<Account>
{
    Task<List<AccountSummary>> GetSummariesAsync(string userId, CancellationToken ct = default);
    Task<AccountBalance?> GetBalanceAsync(Guid accountId, string userId, CancellationToken ct = default);
    Task<List<AccountBalanceData>> GetBalanceDataAsync(string userId, CancellationToken ct = default);
    Task<bool> HasDefaultCurrencyAsync(string currencyCode, CancellationToken ct = default);
}

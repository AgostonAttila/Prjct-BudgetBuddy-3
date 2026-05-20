using BudgetBuddy.Shared.Kernel.Constants;

namespace BudgetBuddy.Service.Transactions.Features.CreateTransaction;

public record CreateTransactionCommand(
    Guid AccountId,
    Guid? CategoryId,
    Guid? TypeId,
    [property: SensitiveData] decimal Amount,
    string CurrencyCode,
    [property: SensitiveData] decimal? RefCurrencyAmount,
    TransactionType TransactionType,
    PaymentType PaymentType,
    [property: SensitiveData(Strategy = MaskingStrategy.Partial)] string? Note,
    LocalDate TransactionDate,
    bool IsTransfer,
    Guid? TransferToAccountId,
    [property: SensitiveData(Strategy = MaskingStrategy.Partial)] string? Payee,
    [property: SensitiveData(Strategy = MaskingStrategy.Partial)] string? Labels
) : IRequest<TransactionResponse>, ICacheInvalidator
{
    public string[] CacheTags => [Tags.Transactions, Tags.AccountBalance, Tags.PortfolioValue, Tags.Dashboard, Tags.MonthlySummary, Tags.IncomeVsExpense, Tags.SpendingByCategory, Tags.BudgetVsActual, Tags.BudgetAlerts];
}

public record TransactionResponse(
    Guid Id,
    Guid AccountId,
    Guid? CategoryId,
    Guid? TypeId,
    decimal Amount,
    string CurrencyCode,
    decimal? RefCurrencyAmount,
    TransactionType TransactionType,
    PaymentType PaymentType,
    string? Note,
    LocalDate TransactionDate,
    bool IsTransfer,
    Guid? TransferToAccountId,
    string? Payee,
    string? Labels
);

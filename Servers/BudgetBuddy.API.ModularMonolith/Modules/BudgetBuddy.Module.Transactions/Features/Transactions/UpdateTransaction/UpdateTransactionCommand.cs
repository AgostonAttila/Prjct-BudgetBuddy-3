using BudgetBuddy.Shared.Kernel.Enums;
using MediatR;
using NodaTime;
using BudgetBuddy.Shared.Kernel.Constants;

namespace BudgetBuddy.Module.Transactions.Features.UpdateTransaction;

public record UpdateTransactionCommand(
    Guid Id,
    Guid? CategoryId,
    Guid? TypeId,
    decimal Amount,
    string CurrencyCode,
    decimal? RefCurrencyAmount,
    TransactionType TransactionType,
    PaymentType PaymentType,
    string? Note,
    LocalDate TransactionDate,
    string? Payee,
    string? Labels
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
    string? Payee,
    string? Labels
);

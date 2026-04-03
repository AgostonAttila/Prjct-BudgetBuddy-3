using BudgetBuddy.API.VSA.Common.Domain.Enums;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using MediatR;
using NodaTime;
using BudgetBuddy.API.VSA.Common.Domain.Constants;

namespace BudgetBuddy.API.VSA.Features.Transactions.UpdateTransaction;

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

using BudgetBuddy.Application.Common.Contracts;
using BudgetBuddy.Domain.Constants;

namespace BudgetBuddy.Application.Features.Investments.CreateInvestment;

public record CreateInvestmentCommand(
    string Symbol,
    string Name,
    InvestmentType Type,
    decimal Quantity,
    decimal PurchasePrice,
    string CurrencyCode,
    LocalDate PurchaseDate,
    string? Note,
    Guid? AccountId
) : IRequest<CreateInvestmentResponse>, ICacheInvalidator
{
    public string[] CacheTags => [Tags.Investments, Tags.PortfolioValue, Tags.InvestmentPerformance, Tags.Dashboard];
}

public record CreateInvestmentResponse(
    Guid Id,
    string Symbol,
    string Name,
    InvestmentType Type,
    decimal Quantity,
    decimal PurchasePrice,
    string CurrencyCode,
    LocalDate PurchaseDate
);

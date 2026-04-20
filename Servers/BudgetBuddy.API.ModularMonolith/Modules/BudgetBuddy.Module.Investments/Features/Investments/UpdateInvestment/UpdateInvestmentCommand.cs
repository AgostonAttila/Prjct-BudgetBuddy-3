using BudgetBuddy.Shared.Kernel.Constants;

namespace BudgetBuddy.Module.Investments.Features.UpdateInvestment;

public record UpdateInvestmentCommand(
    Guid Id,
    string Symbol,
    string Name,
    InvestmentType Type,
    decimal Quantity,
    decimal PurchasePrice,
    string CurrencyCode,
    LocalDate PurchaseDate,
    string? Note,
    Guid? AccountId
) : IRequest<InvestmentResponse>, ICacheInvalidator
{
    public string[] CacheTags => [Tags.Investments, Tags.PortfolioValue, Tags.InvestmentPerformance, Tags.Dashboard];
}

public record InvestmentResponse(
    Guid Id,
    string Symbol,
    string Name,
    InvestmentType Type,
    decimal Quantity,
    decimal PurchasePrice,
    string CurrencyCode,
    LocalDate PurchaseDate,
    string? Note,
    Guid? AccountId
);

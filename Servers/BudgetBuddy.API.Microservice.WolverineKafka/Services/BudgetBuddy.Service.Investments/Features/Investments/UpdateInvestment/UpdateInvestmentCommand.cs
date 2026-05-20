using BudgetBuddy.Shared.Kernel.Constants;

namespace BudgetBuddy.Service.Investments.Features.UpdateInvestment;

public record UpdateInvestmentCommand(
    Guid Id,
    [property: SensitiveData(Strategy = MaskingStrategy.Partial)] string Symbol,
    string Name,
    InvestmentType Type,
    [property: SensitiveData] decimal Quantity,
    [property: SensitiveData] decimal PurchasePrice,
    string CurrencyCode,
    LocalDate PurchaseDate,
    [property: SensitiveData(Strategy = MaskingStrategy.Partial)] string? Note,
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

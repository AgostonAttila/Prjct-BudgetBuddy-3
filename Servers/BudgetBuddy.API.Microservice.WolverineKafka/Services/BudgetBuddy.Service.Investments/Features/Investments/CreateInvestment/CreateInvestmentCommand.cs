using BudgetBuddy.Shared.Kernel.Constants;

namespace BudgetBuddy.Service.Investments.Features.CreateInvestment;

public record CreateInvestmentCommand(
    [property: SensitiveData(Strategy = MaskingStrategy.Partial)] string Symbol,
    string Name,
    InvestmentType Type,
    [property: SensitiveData] decimal Quantity,
    [property: SensitiveData] decimal PurchasePrice,
    string CurrencyCode,
    LocalDate PurchaseDate,
    [property: SensitiveData(Strategy = MaskingStrategy.Partial)] string? Note,
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

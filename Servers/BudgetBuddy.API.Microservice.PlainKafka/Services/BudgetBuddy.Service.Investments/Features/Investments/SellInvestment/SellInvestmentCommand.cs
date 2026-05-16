using NodaTime;

namespace BudgetBuddy.Service.Investments.Features.Investments.SellInvestment;

public record SellInvestmentCommand(
    [property: SensitiveData(Strategy = MaskingStrategy.Partial)] string Symbol,
    [property: SensitiveData] decimal QuantityToSell,
    [property: SensitiveData] decimal SalePrice,
    LocalDate SaleDate) : IRequest<SellInvestmentResponse>;

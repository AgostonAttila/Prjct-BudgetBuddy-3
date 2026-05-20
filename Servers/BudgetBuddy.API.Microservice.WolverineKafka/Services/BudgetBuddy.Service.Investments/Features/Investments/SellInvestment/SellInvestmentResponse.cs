namespace BudgetBuddy.Service.Investments.Features.Investments.SellInvestment;

public record SellInvestmentResponse(
    string Symbol,
    decimal QuantitySold,
    decimal SalePrice,
    decimal RealizedGainLoss);

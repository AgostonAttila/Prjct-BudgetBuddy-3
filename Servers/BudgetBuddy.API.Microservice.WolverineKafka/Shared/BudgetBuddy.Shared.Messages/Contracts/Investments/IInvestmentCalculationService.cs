using BudgetBuddy.Shared.Kernel.Enums;

namespace BudgetBuddy.Shared.Messages.Contracts.Investments;

public interface IInvestmentCalculationService
{
    Task<Dictionary<string, decimal>> GetCurrentPricesAsync(
        List<(string Symbol, InvestmentType Type)> symbolsWithTypes,
        string targetCurrency,
        Dictionary<string, decimal>? purchasePriceFallback = null,
        CancellationToken cancellationToken = default);

    (decimal TotalInvested, decimal CurrentValue, decimal GainLoss, decimal GainLossPercentage)
        CalculateInvestmentMetrics(decimal quantity, decimal purchasePrice, decimal currentPrice);
}

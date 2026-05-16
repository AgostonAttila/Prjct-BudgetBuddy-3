using BudgetBuddy.Shared.Kernel.Enums;

namespace BudgetBuddy.Shared.Messages.Contracts.Financial;

public interface IPriceService
{
    Task<decimal> GetCurrentPriceAsync(
        string symbol,
        InvestmentType investmentType,
        string currencyCode = "USD",
        CancellationToken cancellationToken = default);

    Task<Dictionary<string, decimal>> GetBatchPricesAsync(
        List<(string Symbol, InvestmentType Type)> investments,
        string currencyCode = "USD",
        CancellationToken cancellationToken = default);
}

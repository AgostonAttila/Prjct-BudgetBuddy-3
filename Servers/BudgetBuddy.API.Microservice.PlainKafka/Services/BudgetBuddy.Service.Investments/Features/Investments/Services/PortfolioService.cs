using BudgetBuddy.Service.Investments.Features.GetPortfolioValue;
using BudgetBuddy.Shared.Infrastructure.Financial;
using BudgetBuddy.Shared.Infrastructure.Services;

namespace BudgetBuddy.Service.Investments.Features.Services;

public class PortfolioService(
    InvestmentsDbContext context,
    IInvestmentCalculationService investmentCalculationService,
    ICurrencyConversionService currencyConversionService,
    IAccountBalanceService accountBalanceService,
    ILogger<PortfolioService> logger) : CurrencyServiceBase(currencyConversionService, logger), IPortfolioService
{
    public async Task<(decimal AccountBalance, decimal InvestmentValue, List<PortfolioAccountBalanceDto> AccountBreakdown, List<PortfolioInvestmentValueDto> InvestmentBreakdown)> CalculatePortfolioValueAsync(
        string userId,
        string targetCurrency = "USD",
        CancellationToken cancellationToken = default)
    {
        var accountBreakdown = await CalculateAccountBalancesAsync(userId, targetCurrency, cancellationToken);
        var accountBalance = accountBreakdown.Sum(a => a.ConvertedBalance);

        var investmentBreakdown = await CalculateInvestmentValuesAsync(userId, targetCurrency, cancellationToken);
        var investmentValue = investmentBreakdown.Sum(i => i.TotalValue);

        return (accountBalance, investmentValue, accountBreakdown, investmentBreakdown);
    }

    public async Task<List<PortfolioAccountBalanceDto>> CalculateAccountBalancesAsync(
        string userId,
        string targetCurrency = "USD",
        CancellationToken cancellationToken = default)
    {
        var balances = await accountBalanceService.CalculateAccountBalancesAsync(
            userId,
            targetCurrency,
            upToDate: null,
            cancellationToken);

        return balances
            .Select(b => new PortfolioAccountBalanceDto(
                AccountName: b.AccountName,
                CurrencyCode: b.CurrencyCode,
                Balance: b.Balance,
                ConvertedBalance: b.ConvertedBalance
            ))
            .ToList();
    }

    public async Task<List<PortfolioInvestmentValueDto>> CalculateInvestmentValuesAsync(
        string userId,
        string targetCurrency = "USD",
        CancellationToken cancellationToken = default)
    {
        // Include unsold and partially-sold lots; use remaining quantity for cost basis (#16).
        var investments = await context.Investments
            .AsNoTracking()
            .Where(i => i.UserId == userId &&
                        (i.SoldDate == null || (i.SoldQuantity ?? 0) < i.Quantity))
            .Select(i => new
            {
                i.Symbol,
                i.Name,
                i.Type,
                RemainingQuantity = i.Quantity - (i.SoldQuantity ?? 0),
                i.PurchasePrice,
                i.CurrencyCode
            })
            .ToListAsync(cancellationToken);

        if (investments.Count == 0)
        {
            return [];
        }

        // Pre-fetch conversion rates for all unique purchase currencies
        var conversionRates = await GetExchangeRatesFromCollectionAsync(investments, i => i.CurrencyCode, targetCurrency, cancellationToken);

        // Group by symbol; compute quantity-weighted average purchase price in targetCurrency
        var consolidatedInvestments = investments
            .GroupBy(i => i.Symbol)
            .Select(g =>
            {
                var totalQuantity = g.Sum(i => i.RemainingQuantity);
                var weightedAvgInTargetCurrency = g.Sum(i => i.RemainingQuantity * i.PurchasePrice * conversionRates[i.CurrencyCode]) / totalQuantity;
                return new
                {
                    Symbol = g.Key,
                    Name = g.First().Name,
                    Type = g.First().Type,
                    TotalQuantity = totalQuantity,   // remaining (post-FIFO) quantity
                    WeightedAvgPurchasePrice = weightedAvgInTargetCurrency
                };
            })
            .ToList();

        var symbolsWithTypes = consolidatedInvestments
            .Select(i => (i.Symbol, i.Type))
            .ToList();

        // Fallback chain: live (Financial service HTTP) → PriceSnapshot → 0
        var currentPrices = await investmentCalculationService.GetCurrentPricesAsync(
            symbolsWithTypes, targetCurrency, purchasePriceFallback: null, cancellationToken);

        return consolidatedInvestments
            .Select(investment =>
            {
                var currentPrice = currentPrices.GetValueOrDefault(investment.Symbol, 0m);
                var avgPurchasePrice = Math.Round(investment.WeightedAvgPurchasePrice, 2);

                var (_, currentValue, gainLoss, gainLossPercentage) =
                    investmentCalculationService.CalculateInvestmentMetrics(
                        investment.TotalQuantity,
                        investment.WeightedAvgPurchasePrice,
                        currentPrice);

                return new PortfolioInvestmentValueDto(
                    Symbol: investment.Symbol,
                    Name: investment.Name,
                    Quantity: investment.TotalQuantity,
                    PurchasePrice: avgPurchasePrice,
                    CurrentPrice: Math.Round(currentPrice, 2),
                    TotalValue: Math.Round(currentValue, 2),
                    GainLoss: Math.Round(gainLoss, 2),
                    GainLossPercentage: gainLossPercentage
                );
            })
            .ToList();
    }

}

using BudgetBuddy.Shared.Infrastructure.Financial;
using BudgetBuddy.Shared.Infrastructure.Services;
using BudgetBuddy.Module.Investments.Features.GetPortfolioValue;

namespace BudgetBuddy.Module.Investments.Features.Services;

public class PortfolioService(
    InvestmentsDbContext context,
    IInvestmentCalculationService investmentCalculationService,
    ICurrencyConversionService currencyConversionService,
    IAccountBalanceService accountBalanceService,
    IClock clock,
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
        var investments = await context.Investments
            .AsNoTracking()
            .Where(i => i.UserId == userId && i.SoldDate == null)
            .Select(i => new
            {
                i.Symbol,
                i.Name,
                i.Type,
                i.Quantity,
                i.PurchasePrice,
                i.CurrencyCode
            })
            .ToListAsync(cancellationToken);

        if (investments.Count == 0)
            return [];

        // Pre-fetch conversion rates for all unique purchase currencies
        var conversionRates = await GetExchangeRatesFromCollectionAsync(investments, i => i.CurrencyCode, targetCurrency, cancellationToken);

        // Group by symbol; compute quantity-weighted average purchase price in targetCurrency
        var consolidatedInvestments = investments
            .GroupBy(i => i.Symbol)
            .Select(g =>
            {
                var totalQuantity = g.Sum(i => i.Quantity);
                var weightedAvgInTargetCurrency = g.Sum(i => i.Quantity * i.PurchasePrice * conversionRates[i.CurrencyCode]) / totalQuantity;
                return new
                {
                    Symbol = g.Key,
                    Name = g.First().Name,
                    Type = g.First().Type,
                    TotalQuantity = totalQuantity,
                    WeightedAvgPurchasePrice = weightedAvgInTargetCurrency
                };
            })
            .ToList();

        var symbolsWithTypes = consolidatedInvestments
            .Select(i => (i.Symbol, i.Type))
            .ToList();

        var purchasePriceFallback = consolidatedInvestments
            .ToDictionary(i => i.Symbol, i => i.WeightedAvgPurchasePrice);

        // Fallback chain handled by InvestmentCalculationService: live → snapshot → purchasePriceFallback
        var currentPrices = await investmentCalculationService.GetCurrentPricesAsync(
            symbolsWithTypes, targetCurrency, purchasePriceFallback: null, cancellationToken);

        return consolidatedInvestments
            .Select(investment =>
            {
                var currentPrice = currentPrices.GetValueOrDefault(investment.Symbol, investment.WeightedAvgPurchasePrice);
                var avgPurchasePrice = Math.Round(investment.WeightedAvgPurchasePrice, 2);

                var (totalInvested, currentValue, gainLoss, gainLossPercentage) =
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

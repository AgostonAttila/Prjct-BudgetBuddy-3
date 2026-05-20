namespace BudgetBuddy.Service.Financial.Features.Prices.GetCurrentPrices;

/// <summary>
/// Query for fetching current prices for a list of symbols.
/// Symbols are passed as "SYMBOL:Type" pairs, e.g. "BTC:Crypto,AAPL:Stock".
/// </summary>
public record GetCurrentPricesQuery(
    List<(string Symbol, InvestmentType Type)> SymbolsWithTypes,
    string Currency) : IRequest<Dictionary<string, decimal>>;

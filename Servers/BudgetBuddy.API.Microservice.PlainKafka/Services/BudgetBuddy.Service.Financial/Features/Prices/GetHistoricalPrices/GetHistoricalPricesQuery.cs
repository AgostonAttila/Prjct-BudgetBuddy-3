using BudgetBuddy.Shared.Kernel.Enums;
using NodaTime;

namespace BudgetBuddy.Service.Financial.Features.Prices.GetHistoricalPrices;

public record GetHistoricalPricesQuery(
    string Symbol,
    InvestmentType Type,
    LocalDate From) : IRequest<Dictionary<string, decimal>>;

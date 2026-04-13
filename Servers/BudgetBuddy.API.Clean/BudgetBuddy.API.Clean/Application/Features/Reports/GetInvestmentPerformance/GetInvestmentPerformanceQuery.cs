namespace BudgetBuddy.Application.Features.Reports.GetInvestmentPerformance;

public record GetInvestmentPerformanceQuery(
    LocalDate? StartDate = null,
    LocalDate? EndDate = null,
    InvestmentType? Type = null,
    string? DisplayCurrency = null
) : IRequest<InvestmentPerformanceResponse>;

public record InvestmentPerformanceResponse(
    decimal TotalInvested,
    decimal CurrentValue,
    decimal TotalGainLoss,
    decimal TotalGainLossPercentage,
    List<InvestmentPerformance> Investments,
    string Currency
);

public record InvestmentPerformance(
    Guid InvestmentId,
    string Symbol,
    string Name,
    InvestmentType Type,
    decimal Quantity,
    decimal PurchasePrice,
    decimal TotalInvested,
    decimal CurrentPrice,
    decimal CurrentValue,
    decimal GainLoss,
    decimal GainLossPercentage,
    int DaysHeld,
    LocalDate PurchaseDate
);

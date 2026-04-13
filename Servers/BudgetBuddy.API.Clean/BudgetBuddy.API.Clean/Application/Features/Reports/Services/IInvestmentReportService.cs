using BudgetBuddy.Application.Features.Reports.GetInvestmentPerformance;

namespace BudgetBuddy.Application.Features.Reports.Services;

public interface IInvestmentReportService
{
    Task<(decimal TotalInvested, decimal CurrentValue, decimal TotalGainLoss, decimal TotalGainLossPercentage, List<InvestmentPerformance> Investments)>
        CalculateInvestmentPerformanceAsync(
            string userId,
            LocalDate? startDate = null,
            LocalDate? endDate = null,
            InvestmentType? type = null,
            string displayCurrency = "USD",
            CancellationToken cancellationToken = default);
}

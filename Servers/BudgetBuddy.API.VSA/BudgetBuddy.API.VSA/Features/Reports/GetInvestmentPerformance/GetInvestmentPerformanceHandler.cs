using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Features.Reports.Services;

namespace BudgetBuddy.API.VSA.Features.Reports.GetInvestmentPerformance;

public class GetInvestmentPerformanceHandler(
    IInvestmentReportService reportService,
    ICurrentUserService currentUserService,
    IUserCurrencyService userCurrencyService,
    ILogger<GetInvestmentPerformanceHandler> logger)
    : ReportHandlerBase<GetInvestmentPerformanceQuery, InvestmentPerformanceResponse>(
        currentUserService, userCurrencyService, logger)
{
    protected override string? GetDisplayCurrency(GetInvestmentPerformanceQuery request) => request.DisplayCurrency;

    protected override async Task<InvestmentPerformanceResponse> HandleCoreAsync(
        string userId,
        string displayCurrency,
        GetInvestmentPerformanceQuery request,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation(
            "Getting investment performance for user {UserId}, period: {StartDate} - {EndDate}, type: {Type}, currency: {Currency}",
            userId, request.StartDate, request.EndDate, request.Type, displayCurrency);

        var (totalInvested, currentValue, totalGainLoss, totalGainLossPercentage, investments) =
            await reportService.CalculateInvestmentPerformanceAsync(
                userId, request.StartDate, request.EndDate, request.Type, displayCurrency, cancellationToken);

        Logger.LogInformation(
            "Investment performance calculated: Invested={Invested}, Current={Current}, Gain/Loss={GainLoss} ({Percentage}%)",
            totalInvested, currentValue, totalGainLoss, totalGainLossPercentage);

        var roundedInvestments = investments.Select(i => i with
        {
            TotalInvested = Math.Round(i.TotalInvested, 2),
            CurrentValue = Math.Round(i.CurrentValue, 2),
            GainLoss = Math.Round(i.GainLoss, 2)
        }).ToList();

        return new InvestmentPerformanceResponse(
            totalInvested,
            currentValue,
            totalGainLoss,
            totalGainLossPercentage,
            roundedInvestments,
            displayCurrency
        );
    }
}

using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;
using BudgetBuddy.Module.Investments.Features.Services;

namespace BudgetBuddy.Module.Investments.Features.GetPortfolioValue;

public class GetPortfolioValueHandler(
    IPortfolioService portfolioService,
    ICurrentUserService currentUserService,
    IUserCurrencyService userCurrencyService,
    ILogger<GetPortfolioValueHandler> logger) : UserAwareHandler<GetPortfolioValueQuery, PortfolioValueResponse>(currentUserService)
{
    public override async Task<PortfolioValueResponse> Handle(
        GetPortfolioValueQuery request,
        CancellationToken cancellationToken)
    {
        var targetCurrency = await userCurrencyService.GetDisplayCurrencyAsync(request.TargetCurrency, cancellationToken);

        logger.LogInformation("Calculating portfolio value for user {UserId}, currency: {Currency}", UserId, targetCurrency);

        var (accountBalance, investmentValue, accountBreakdown, investmentBreakdown) = await portfolioService.CalculatePortfolioValueAsync(
            UserId,
            targetCurrency,
            cancellationToken);

        var totalPortfolioValue = accountBalance + investmentValue;

        logger.LogInformation(
            "Portfolio value calculated: Accounts={AccountBalance}, Investments={InvestmentValue}, Total={TotalValue}",
            accountBalance,
            investmentValue,
            totalPortfolioValue);

        return new PortfolioValueResponse(
            TotalAccountBalance: accountBalance,
            TotalInvestmentValue: investmentValue,
            TotalPortfolioValue: totalPortfolioValue,
            Currency: targetCurrency,
            AccountBreakdown: accountBreakdown,
            InvestmentBreakdown: investmentBreakdown
        );
    }
}

using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Shared.Handlers;
using BudgetBuddy.API.VSA.Features.Reports.Services;

namespace BudgetBuddy.API.VSA.Features.Reports.GetSpendingByCategory;

public class GetSpendingByCategoryHandler(
    ISpendingReportService reportService,
    ICurrentUserService currentUserService,
    ILogger<GetSpendingByCategoryHandler> logger) : UserAwareHandler<GetSpendingByCategoryQuery, SpendingByCategoryResponse>(currentUserService)
{
    public override async Task<SpendingByCategoryResponse> Handle(
        GetSpendingByCategoryQuery request,
        CancellationToken cancellationToken)
    {
        var displayCurrency = request.DisplayCurrency?.ToUpperInvariant() ?? "USD";

        logger.LogInformation(
            "Getting spending by category for user {UserId}, period: {StartDate} - {EndDate}, currency: {Currency}",
            UserId,
            request.StartDate,
            request.EndDate,
            displayCurrency);

        var (totalSpending, categories) = await reportService.CalculateSpendingByCategoryAsync(
            UserId,
            request.StartDate,
            request.EndDate,
            request.AccountId,
            displayCurrency,
            cancellationToken);

        logger.LogInformation(
            "Spending by category calculated: Total={Total}, Categories={Count}",
            totalSpending,
            categories.Count);

        return new SpendingByCategoryResponse(totalSpending, categories, displayCurrency);
    }
}

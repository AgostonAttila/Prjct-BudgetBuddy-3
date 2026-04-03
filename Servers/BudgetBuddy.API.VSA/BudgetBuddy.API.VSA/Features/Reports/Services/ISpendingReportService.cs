using BudgetBuddy.API.VSA.Features.Reports.GetSpendingByCategory;

namespace BudgetBuddy.API.VSA.Features.Reports.Services;

public interface ISpendingReportService
{
    Task<(decimal TotalSpending, List<CategorySpending> Categories)>
        CalculateSpendingByCategoryAsync(
            string userId,
            LocalDate? startDate = null,
            LocalDate? endDate = null,
            Guid? accountId = null,
            string displayCurrency = "USD",
            CancellationToken cancellationToken = default);
}

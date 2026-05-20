namespace BudgetBuddy.Service.Analytics.Features.Reports.Services;

public interface IReportService
    : IIncomeExpenseReportService,
      IMonthlySummaryReportService,
      ISpendingReportService,
      IInvestmentReportService;

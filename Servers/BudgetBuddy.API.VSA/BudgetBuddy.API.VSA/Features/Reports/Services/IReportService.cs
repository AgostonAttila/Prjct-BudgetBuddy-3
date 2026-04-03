namespace BudgetBuddy.API.VSA.Features.Reports.Services;

public interface IReportService
    : IIncomeExpenseReportService,
      IMonthlySummaryReportService,
      ISpendingReportService,
      IInvestmentReportService;

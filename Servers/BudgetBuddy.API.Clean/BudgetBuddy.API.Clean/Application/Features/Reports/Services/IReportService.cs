namespace BudgetBuddy.Application.Features.Reports.Services;

public interface IReportService
    : IIncomeExpenseReportService,
      IMonthlySummaryReportService,
      ISpendingReportService,
      IInvestmentReportService;

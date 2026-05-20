namespace BudgetBuddy.Service.Budgets.Features.BudgetAlerts.Services;

public interface IBudgetAlertService
    : IBudgetAlertCalculationService,
      IBudgetAlertRuleEngine,
      IBudgetAlertMessageFormatter;

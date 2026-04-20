namespace BudgetBuddy.Module.Budgets.Features.BudgetAlerts.Services;

public interface IBudgetAlertService
    : IBudgetAlertCalculationService,
      IBudgetAlertRuleEngine,
      IBudgetAlertMessageFormatter;

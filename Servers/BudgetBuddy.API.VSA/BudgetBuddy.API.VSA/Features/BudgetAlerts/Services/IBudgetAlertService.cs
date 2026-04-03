namespace BudgetBuddy.API.VSA.Features.BudgetAlerts.Services;

public interface IBudgetAlertService
    : IBudgetAlertCalculationService,
      IBudgetAlertRuleEngine,
      IBudgetAlertMessageFormatter;

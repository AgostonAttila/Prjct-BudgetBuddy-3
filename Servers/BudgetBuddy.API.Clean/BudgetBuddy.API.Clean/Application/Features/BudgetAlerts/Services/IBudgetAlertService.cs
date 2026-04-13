namespace BudgetBuddy.Application.Features.BudgetAlerts.Services;

public interface IBudgetAlertService
    : IBudgetAlertCalculationService,
      IBudgetAlertRuleEngine,
      IBudgetAlertMessageFormatter;

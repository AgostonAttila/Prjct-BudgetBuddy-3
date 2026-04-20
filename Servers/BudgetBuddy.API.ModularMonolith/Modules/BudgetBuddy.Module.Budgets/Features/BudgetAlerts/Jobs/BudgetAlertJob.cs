using BudgetBuddy.Shared.Infrastructure.BackgroundJobs;
using BudgetBuddy.Module.Budgets.Features.BudgetAlerts.Services;
using Quartz;

namespace BudgetBuddy.Module.Budgets.Jobs;

/// <summary>
/// Checks all active budgets and sends alert emails when thresholds are exceeded.
/// Runs daily via Quartz scheduler (configurable via BackgroundJobs:BudgetAlerts:CronExpression).
/// </summary>
public class BudgetAlertJob(IBudgetAlertCalculationService budgetAlertService, ILogger<BudgetAlertJob> logger)
    : ScheduledJobBase(logger)
{
    private readonly IBudgetAlertCalculationService _budgetAlertService = budgetAlertService;

    protected override async Task ExecuteAsync(IJobExecutionContext context)
    {
        // TODO: iterate users/budgets and send alert emails via IBudgetAlertService
        await Task.CompletedTask;
    }
}

using BudgetBuddy.Shared.Infrastructure.BackgroundJobs;
using Quartz;

namespace BudgetBuddy.Service.Budgets.Jobs;

public static class BudgetsOutboxProcessorJobExtensions
{
    public static IServiceCollectionQuartzConfigurator AddBudgetsOutboxProcessorJob(
        this IServiceCollectionQuartzConfigurator q, IConfiguration config)
    {
        var cron = config["BackgroundJobs:BudgetsOutboxProcessor:CronExpression"] ?? "0/30 * * * * ?";
        return q.AddJobWithCronTrigger<BudgetsOutboxProcessorJob>(cron);
    }
}

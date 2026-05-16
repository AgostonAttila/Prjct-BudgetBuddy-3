using BudgetBuddy.Shared.Infrastructure.BackgroundJobs;
using Quartz;

namespace BudgetBuddy.Service.Investments.Jobs;

public static class InvestmentsOutboxProcessorJobExtensions
{
    public static IServiceCollectionQuartzConfigurator AddInvestmentsOutboxProcessorJob(
        this IServiceCollectionQuartzConfigurator q, IConfiguration config)
    {
        var cron = config["BackgroundJobs:InvestmentsOutboxProcessor:CronExpression"] ?? "0/30 * * * * ?";
        return q.AddJobWithCronTrigger<InvestmentsOutboxProcessorJob>(cron);
    }
}

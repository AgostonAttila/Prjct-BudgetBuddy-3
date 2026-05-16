using BudgetBuddy.Shared.Infrastructure.BackgroundJobs;
using Quartz;

namespace BudgetBuddy.Service.Accounts.Jobs;

public static class AccountsOutboxProcessorJobExtensions
{
    public static IServiceCollectionQuartzConfigurator AddAccountsOutboxProcessorJob(
        this IServiceCollectionQuartzConfigurator q, IConfiguration config)
    {
        var cron = config["BackgroundJobs:AccountsOutboxProcessor:CronExpression"] ?? "0/30 * * * * ?";
        return q.AddJobWithCronTrigger<AccountsOutboxProcessorJob>(cron);
    }
}

using BudgetBuddy.Shared.Infrastructure.BackgroundJobs;
using BudgetBuddy.Shared.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Quartz;

namespace BudgetBuddy.Module.Transactions.Features.Transactions.Jobs;

public static class TransactionsOutboxProcessorJobExtensions
{
    private const string ConfigSection = "BackgroundJobs:TransactionsOutboxProcessor";

    public static IServiceCollectionQuartzConfigurator AddTransactionsOutboxProcessorJob(
        this IServiceCollectionQuartzConfigurator q, IConfiguration configuration)
    {
        var settings = configuration
            .GetSection(ConfigSection)
            .Get<JobSettings>() ?? new JobSettings
        {
            Enabled = true,
            // Every 30 seconds
            CronExpression = "0/30 * * * * ?"
        };

        if (settings.Enabled)
            q.AddJobWithCronTrigger<TransactionsOutboxProcessorJob>(settings.CronExpression);

        return q;
    }
}

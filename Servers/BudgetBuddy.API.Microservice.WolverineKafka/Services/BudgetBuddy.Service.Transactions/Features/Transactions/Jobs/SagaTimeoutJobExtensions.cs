using BudgetBuddy.Shared.Infrastructure.BackgroundJobs;
using BudgetBuddy.Shared.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Quartz;

namespace BudgetBuddy.Service.Transactions.Features.Transactions.Jobs;

public static class SagaTimeoutJobExtensions
{
    private const string ConfigSection = "BackgroundJobs:SagaTimeout";

    public static IServiceCollectionQuartzConfigurator AddSagaTimeoutJob(
        this IServiceCollectionQuartzConfigurator q, IConfiguration configuration)
    {
        var settings = configuration
            .GetSection(ConfigSection)
            .Get<JobSettings>() ?? new JobSettings
        {
            Enabled = true,
            // Every 30 seconds — aligns with the TimeoutSeconds constant in SagaTimeoutJob
            CronExpression = "0/30 * * * * ?"
        };

        if (settings.Enabled)
        {
            q.AddJobWithCronTrigger<SagaTimeoutJob>(settings.CronExpression);
        }

        return q;
    }
}

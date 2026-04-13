using BudgetBuddy.Infrastructure.BackgroundJobs;
using Quartz;

namespace BudgetBuddy.Infrastructure.BackgroundJobs.BudgetAlerts;

public static class BudgetAlertJobExtensions
{
    private const string ConfigSection = "BackgroundJobs:BudgetAlerts";

    public static IServiceCollectionQuartzConfigurator AddBudgetAlertJob(
        this IServiceCollectionQuartzConfigurator q, IConfiguration configuration)
    {
        var settings = configuration
            .GetSection(ConfigSection)
            .Get<JobSettings>() ?? new();

        if (settings.Enabled)
            q.AddJobWithCronTrigger<BudgetAlertJob>(settings.CronExpression);

        return q;
    }
}

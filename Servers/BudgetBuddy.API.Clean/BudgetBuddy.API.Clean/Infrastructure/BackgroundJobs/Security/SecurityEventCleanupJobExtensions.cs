using BudgetBuddy.Infrastructure.BackgroundJobs;
using Quartz;

namespace BudgetBuddy.Infrastructure.BackgroundJobs.Security;

public class SecurityEventCleanupJobSettings : JobSettings
{
    public int RetentionDays { get; init; } = 90;
}

public static class SecurityEventCleanupJobExtensions
{
    private const string ConfigSection = "BackgroundJobs:SecurityEventCleanup";

    public static IServiceCollectionQuartzConfigurator AddSecurityEventCleanupJob(
        this IServiceCollectionQuartzConfigurator q, IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration
            .GetSection(ConfigSection)
            .Get<SecurityEventCleanupJobSettings>() ?? new();

        if (!settings.Enabled)
            return q;

        services.AddSingleton(settings);
        q.AddJobWithCronTrigger<SecurityEventCleanupJob>(settings.CronExpression);

        return q;
    }
}

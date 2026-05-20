using BudgetBuddy.Shared.Infrastructure.BackgroundJobs;
using BudgetBuddy.Shared.Infrastructure.Persistence;
using Quartz;

namespace BudgetBuddy.Shared.Infrastructure.Extensions;

public static class BackgroundJobsExtensions
{
    public static IServiceCollection AddBackgroundJobs(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IServiceCollectionQuartzConfigurator>? configureJobs = null)
    {
        var settings = configuration
            .GetSection(BackgroundJobsSettings.SectionName)
            .Get<BackgroundJobsSettings>() ?? new();

        if (!settings.Enabled)
        {
            return services;
        }

        services.AddQuartz(q => configureJobs?.Invoke(q));
        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

        return services;
    }

    /// <summary>
    /// S-08: Registers the audit log retention job for the given DbContext.
    /// Defaults to daily at 02:00 UTC; override via <c>AuditLog:RetentionCron</c>.
    /// </summary>
    public static IServiceCollectionQuartzConfigurator AddAuditLogRetentionJob<TDbContext>(
        this IServiceCollectionQuartzConfigurator q, IConfiguration configuration)
        where TDbContext : AppDbContext
    {
        var cron = configuration["AuditLog:RetentionCron"] ?? "0 0 2 * * ?";
        return q.AddJobWithCronTrigger<AuditLogRetentionJob<TDbContext>>(cron);
    }

    /// <summary>Registers a job with a cron trigger using the job type name as identity.</summary>
    public static IServiceCollectionQuartzConfigurator AddJobWithCronTrigger<TJob>(
        this IServiceCollectionQuartzConfigurator q, string cronExpression)
        where TJob : IJob
    {
        var jobKey = new JobKey(typeof(TJob).Name);

        q.AddJob<TJob>(opts => opts.WithIdentity(jobKey));

        q.AddTrigger(opts => opts
            .ForJob(jobKey)
            .WithIdentity($"{typeof(TJob).Name}-trigger")
            .WithCronSchedule(cronExpression, x => x.WithMisfireHandlingInstructionDoNothing()));

        return q;
    }
}

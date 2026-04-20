using BudgetBuddy.Shared.Infrastructure.BackgroundJobs;
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
            return services;

        services.AddQuartz(q => configureJobs?.Invoke(q));
        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

        return services;
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

using Quartz;

namespace BudgetBuddy.Infrastructure.BackgroundJobs;

/// <summary>
/// Base class for all background jobs. Wraps execution with structured logging and error handling.
/// Derive from this class and implement <see cref="ExecuteAsync"/> to create a new job.
/// </summary>
[DisallowConcurrentExecution]
public abstract class ScheduledJobBase(ILogger logger) : IJob
{
    protected readonly ILogger Logger = logger;

    public async Task Execute(IJobExecutionContext context)
    {
        var jobName = GetType().Name;
        Logger.LogInformation("Background job {JobName} started at {FireTime}", jobName, context.FireTimeUtc);

        try
        {
            await ExecuteAsync(context);
            Logger.LogInformation("Background job {JobName} completed successfully", jobName);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Background job {JobName} failed", jobName);
            throw new JobExecutionException(ex, refireImmediately: false);
        }
    }

    protected abstract Task ExecuteAsync(IJobExecutionContext context);
}

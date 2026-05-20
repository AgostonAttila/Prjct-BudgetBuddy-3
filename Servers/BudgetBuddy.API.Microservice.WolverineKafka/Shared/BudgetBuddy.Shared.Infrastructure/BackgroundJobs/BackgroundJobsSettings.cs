namespace BudgetBuddy.Shared.Infrastructure.BackgroundJobs;

public class BackgroundJobsSettings
{
    public const string SectionName = "BackgroundJobs";

    /// <summary>Master switch — set to false to disable all background jobs (useful in dev/test).</summary>
    public bool Enabled { get; init; } = true;
}

/// <summary>
/// Base settings for any scheduled job. Each feature defines its own section under "BackgroundJobs".
/// </summary>
public class JobSettings
{
    public bool Enabled { get; init; } = true;
    public string CronExpression { get; init; } = "0 0 8 * * ?";
}

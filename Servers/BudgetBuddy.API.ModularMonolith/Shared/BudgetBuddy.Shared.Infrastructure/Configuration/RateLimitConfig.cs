using System.ComponentModel.DataAnnotations;

namespace BudgetBuddy.Shared.Infrastructure.Configuration;

public class RateLimitConfig
{
    public const string SectionName = "RateLimitConfig";

    [Required] public FixedWindowOptions Fixed { get; set; } = new();
    [Required] public FixedWindowOptions FixedByIp { get; set; } = new();
    [Required] public TokenBucketOptions Auth { get; set; } = new();
    [Required] public FixedWindowOptions Global { get; set; } = new();
    [Required] public SlidingWindowOptions Api { get; set; } = new();
    [Required] public FixedWindowOptions Refresh { get; set; } = new();
}

public class FixedWindowOptions
{
    [Range(1, int.MaxValue, ErrorMessage = "PermitLimit must be at least 1.")]
    public int PermitLimit { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "WindowMinutes must be at least 1.")]
    public int WindowMinutes { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "QueueLimit must be 0 or greater.")]
    public int QueueLimit { get; set; }
}

public class TokenBucketOptions
{
    [Range(1, int.MaxValue, ErrorMessage = "TokenLimit must be at least 1.")]
    public int TokenLimit { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "ReplenishmentPeriodMinutes must be at least 1.")]
    public int ReplenishmentPeriodMinutes { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "TokensPerPeriod must be at least 1.")]
    public int TokensPerPeriod { get; set; }
}

public class SlidingWindowOptions
{
    [Range(1, int.MaxValue, ErrorMessage = "PermitLimit must be at least 1.")]
    public int PermitLimit { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "WindowMinutes must be at least 1.")]
    public int WindowMinutes { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "SegmentsPerWindow must be at least 1.")]
    public int SegmentsPerWindow { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "QueueLimit must be 0 or greater.")]
    public int QueueLimit { get; set; }
}
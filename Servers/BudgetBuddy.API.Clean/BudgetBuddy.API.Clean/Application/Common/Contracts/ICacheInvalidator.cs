namespace BudgetBuddy.Application.Common.Contracts;

/// <summary>
/// Marks a command as requiring cache invalidation upon successful execution.
/// </summary>
public interface ICacheInvalidator
{
    /// <summary>
    /// Cache tags to invalidate when this command executes successfully.
    /// Tags correspond to cache policy names in CachingExtensions.cs
    /// </summary>
    string[] CacheTags { get; }
}

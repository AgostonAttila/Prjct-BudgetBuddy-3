namespace BudgetBuddy.Application.Common.Contracts;

/// <summary>
/// Abstracts cache tag eviction so Application layer does not depend on ASP.NET Core Output Cache.
/// </summary>
public interface ICacheTagInvalidator
{
    Task EvictByTagAsync(string tag, CancellationToken cancellationToken);
}

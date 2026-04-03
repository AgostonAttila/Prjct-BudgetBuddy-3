namespace BudgetBuddy.API.VSA.Common.Infrastructure;

/// <summary>
/// Service for retrieving the current authenticated user's information
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current authenticated user's ID (from NameIdentifier claim)
    /// </summary>
    /// <returns>The user ID as a string</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when user is not authenticated</exception>
    string GetCurrentUserId();

    /// <summary>
    /// Gets the current authenticated user's ID or null if not authenticated
    /// </summary>
    /// <returns>The user ID as a string, or null if not authenticated</returns>
    string? GetCurrentUserIdOrNull();
}

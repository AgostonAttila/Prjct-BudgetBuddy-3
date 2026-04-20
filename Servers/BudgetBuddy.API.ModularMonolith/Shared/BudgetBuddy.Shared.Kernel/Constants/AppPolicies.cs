namespace BudgetBuddy.Shared.Kernel.Constants;

/// <summary>
/// Application authorization policy constants
/// </summary>
public static class AppPolicies
{
    /// <summary>
    /// Requires Admin role
    /// </summary>
    public const string AdminOnly = "AdminOnly";

    /// <summary>
    /// Requires Admin or Premium role
    /// </summary>
    public const string AdminOrPremium = "AdminOrPremium";

    /// <summary>
    /// Requires any authenticated user
    /// </summary>
    public const string Authenticated = "Authenticated";

    /// <summary>
    /// Requires 2FA to be enabled
    /// </summary>
    public const string Require2FA = "Require2FA";
}

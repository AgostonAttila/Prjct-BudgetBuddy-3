namespace BudgetBuddy.Domain.Constants;

/// <summary>
/// Application role constants
/// </summary>
public static class AppRoles
{
    /// <summary>
    /// Administrator role - full system access
    /// </summary>
    public const string Admin = "Admin";

    /// <summary>
    /// Standard user role - basic application access
    /// </summary>
    public const string User = "User";

    /// <summary>
    /// Premium user role - advanced features access
    /// </summary>
    public const string Premium = "Premium";

    /// <summary>
    /// All available roles
    /// </summary>
    public static readonly string[] AllRoles = { Admin, User, Premium };
}

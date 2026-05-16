namespace BudgetBuddy.Shared.Kernel.Identity;

/// <summary>
/// Lightweight user value object — populated from Keycloak JWT claims.
/// Not persisted; each service stores only UserId (string) as a plain column.
/// </summary>
public class User
{
    public string Id { get; set; } = string.Empty;         // Keycloak 'sub' claim
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string PreferredLanguage { get; set; } = "en-US";
    public string DefaultCurrency { get; set; } = "USD";
    public string DateFormat { get; set; } = "yyyy-MM-dd";
    public IList<string> Roles { get; set; } = [];
}

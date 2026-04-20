using Microsoft.AspNetCore.Identity;

namespace BudgetBuddy.Shared.Kernel.Entities;

public class User : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string PreferredLanguage { get; set; } = "en-US";
    public string DefaultCurrency { get; set; } = "USD";
    public string DateFormat { get; set; } = "yyyy-MM-dd"; // ISO 8601

    // Cross-module navigations intentionally omitted (Account, Category, Investment)
    // Each module owns its entities — use module DbContext + UserId FK for queries
}

using Microsoft.AspNetCore.Identity;

namespace BudgetBuddy.API.VSA.Common.Domain.Entities;

public class User : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string PreferredLanguage { get; set; } = "en-US";
    public string DefaultCurrency { get; set; } = "USD";
    public string DateFormat { get; set; } = "yyyy-MM-dd"; // ISO 8601

    public ICollection<Account> Accounts { get; set; } = [];
    public ICollection<Category> Categories { get; set; } = [];
    public ICollection<Investment> Investments { get; set; } = [];
}

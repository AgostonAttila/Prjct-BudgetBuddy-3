using BudgetBuddy.Shared.Kernel.Contracts;
using BudgetBuddy.Shared.Kernel.Entities;

namespace BudgetBuddy.Module.Accounts.Domain;

public class Account : AuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DefaultCurrencyCode { get; set; } = "HUF";
    public decimal InitialBalance { get; set; }

    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;

    // Cross-module navigations (Transaction, Investment) intentionally omitted — use FK only
}

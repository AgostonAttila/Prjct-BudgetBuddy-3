using BudgetBuddy.Shared.Kernel.Contracts;

namespace BudgetBuddy.Service.Accounts.Domain;

public class Account : AuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DefaultCurrencyCode { get; set; } = "HUF";
    public decimal InitialBalance { get; set; }

    public string UserId { get; set; } = string.Empty;
}

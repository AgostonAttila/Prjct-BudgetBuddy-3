using BudgetBuddy.Shared.Kernel.Contracts;

namespace BudgetBuddy.Service.Budgets.Domain;

public class Budget : AuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Cross-module FK to referencedata schema — no EF navigation
    public Guid CategoryId { get; set; }

    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public int Year { get; set; }
    public int Month { get; set; }

    public string UserId { get; set; } = string.Empty;
}

using BudgetBuddy.Shared.Kernel.Contracts;
using BudgetBuddy.Shared.Kernel.Entities;

namespace BudgetBuddy.Module.ReferenceData.Domain;

public class Category : AuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Color { get; set; }

    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;

    // Internal navigation (same module)
    public ICollection<CategoryType> Types { get; set; } = [];

    // Cross-module navigations (Transaction) intentionally omitted — use FK only
}

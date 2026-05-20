using BudgetBuddy.Shared.Kernel.Contracts;

namespace BudgetBuddy.Service.ReferenceData.Domain;

public class Category : AuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Color { get; set; }

    public string UserId { get; set; } = string.Empty;

    // Internal navigation (same module)
    public ICollection<CategoryType> Types { get; set; } = [];
}

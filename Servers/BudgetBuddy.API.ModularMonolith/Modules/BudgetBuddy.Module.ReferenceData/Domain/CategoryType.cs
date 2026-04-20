using System.Text.Json.Serialization;
using BudgetBuddy.Shared.Kernel.Contracts;

namespace BudgetBuddy.Module.ReferenceData.Domain;

public class CategoryType : AuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Color { get; set; }

    [JsonIgnore]
    public Guid CategoryId { get; set; }
    [JsonIgnore]
    public Category Category { get; set; } = null!;

    // Cross-module navigations (Transaction) intentionally omitted — use FK only
}

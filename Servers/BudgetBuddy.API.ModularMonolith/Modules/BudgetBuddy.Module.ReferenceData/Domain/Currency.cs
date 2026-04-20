using BudgetBuddy.Shared.Kernel.Contracts;

namespace BudgetBuddy.Module.ReferenceData.Domain;

public class Currency : AuditableEntity
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

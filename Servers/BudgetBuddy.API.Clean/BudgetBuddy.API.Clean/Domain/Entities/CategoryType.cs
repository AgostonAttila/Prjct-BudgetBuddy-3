using BudgetBuddy.Domain.Contracts;

namespace BudgetBuddy.Domain.Entities;

public class CategoryType : AuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Color { get; set; }

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public ICollection<Transaction> Transactions { get; set; } = [];
}

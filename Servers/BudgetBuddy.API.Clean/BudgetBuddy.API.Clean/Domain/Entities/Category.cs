using BudgetBuddy.Domain.Contracts;

namespace BudgetBuddy.Domain.Entities;

public class Category : AuditableEntity, IUserOwnedEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Color { get; set; }

    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;

    public ICollection<CategoryType> Types { get; set; } = [];
    public ICollection<Transaction> Transactions { get; set; } = [];
}

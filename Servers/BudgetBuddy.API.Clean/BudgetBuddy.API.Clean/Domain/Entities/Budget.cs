using BudgetBuddy.Domain.Contracts;

namespace BudgetBuddy.Domain.Entities;

public class Budget : AuditableEntity, IUserOwnedEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public decimal Amount { get; set; } // Budget limit
    public string CurrencyCode { get; set; } = "USD";

    // Budget period
    public int Year { get; set; }
    public int Month { get; set; }

    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;
}

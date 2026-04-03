using BudgetBuddy.API.VSA.Common.Domain.Contracts;

namespace BudgetBuddy.API.VSA.Common.Domain.Entities;

public class Budget : AuditableEntity
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

using BudgetBuddy.Domain.Contracts;

namespace BudgetBuddy.Domain.Entities;

public class Account : AuditableEntity, IUserOwnedEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DefaultCurrencyCode { get; set; } = "HUF";
    public decimal InitialBalance { get; set; }

    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;

    public ICollection<Transaction> Transactions { get; set; } = [];
    public ICollection<Investment> Investments { get; set; } = [];
}

using BudgetBuddy.Shared.Kernel.Contracts;
using BudgetBuddy.Shared.Kernel.Entities;
using BudgetBuddy.Shared.Kernel.Enums;
using NodaTime;

namespace BudgetBuddy.Module.Investments.Domain;

public class Investment : AuditableEntity, IUserOwnedEntity
{
    public Guid Id { get; set; }

    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public InvestmentType Type { get; set; }

    public decimal Quantity { get; set; }
    public decimal PurchasePrice { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public LocalDate PurchaseDate { get; set; }

    public string? Note { get; set; }

    /// <summary>
    /// Date the investment was sold. Null means still held.
    /// </summary>
    public LocalDate? SoldDate { get; set; }

    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;

    // Optional FK to Account (cross-module — navigation intentionally omitted)
    public Guid? AccountId { get; set; }
}

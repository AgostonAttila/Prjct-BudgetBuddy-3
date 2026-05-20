using BudgetBuddy.Shared.Kernel.Contracts;
using BudgetBuddy.Shared.Kernel.Enums;
using NodaTime;

namespace BudgetBuddy.Service.Investments.Domain;

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
    /// Set by <c>SellInvestmentHandler</c> when the lot is fully consumed.
    /// </summary>
    public LocalDate? SoldDate { get; set; }

    /// <summary>
    /// Quantity sold from this lot so far (FIFO allocation).
    /// Null / 0 = unsold. When SoldQuantity reaches Quantity, SoldDate is also set.
    /// </summary>
    public decimal? SoldQuantity { get; set; }

    /// <summary>Price per unit at which this lot was (last) sold.</summary>
    public decimal? SalePrice { get; set; }

    public string UserId { get; set; } = string.Empty;

    // Optional FK to Account (cross-module — navigation intentionally omitted)
    public Guid? AccountId { get; set; }
}

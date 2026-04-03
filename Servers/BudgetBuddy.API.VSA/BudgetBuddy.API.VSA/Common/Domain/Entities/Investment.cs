using BudgetBuddy.API.VSA.Common.Domain.Contracts;

namespace BudgetBuddy.API.VSA.Common.Domain.Entities;

public class Investment : AuditableEntity, IUserOwnedEntity
{
    public Guid Id { get; set; }

    public string Symbol { get; set; } = string.Empty; // BTC, AAPL, VOO, etc.
    public string Name { get; set; } = string.Empty; // Bitcoin, Apple Inc, Vanguard S&P 500 ETF
    public InvestmentType Type { get; set; } // Crypto, ETF, Stock

    public decimal Quantity { get; set; }
    public decimal PurchasePrice { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public LocalDate PurchaseDate { get; set; }

    public string? Note { get; set; }

    /// <summary>
    /// Date the investment was sold. Null means still held.
    /// Queries should filter SoldDate == null to show only active positions.
    /// </summary>
    public LocalDate? SoldDate { get; set; }


    // Multi-user support
    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;

    // Optional: Link to account if purchased through account
    public Guid? AccountId { get; set; }
    public Account? Account { get; set; }
}

using BudgetBuddy.Shared.Kernel.Contracts;
using BudgetBuddy.Shared.Kernel.Enums;
using BudgetBuddy.Shared.Kernel.Logging;
using NodaTime;

namespace BudgetBuddy.Module.Transactions.Domain;

public class Transaction : AuditableEntity, IUserOwnedEntity
{
    public Guid Id { get; set; }

    // Cross-module FK to accounts schema — no EF navigation
    public Guid AccountId { get; set; }

    // Cross-module FK to referencedata schema — no EF navigation
    public Guid? CategoryId { get; set; }
    public Guid? TypeId { get; set; }

    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal? RefCurrencyAmount { get; set; }

    public TransactionType TransactionType { get; set; }
    public PaymentType PaymentType { get; set; }

    [SensitiveData(Strategy = MaskingStrategy.Partial)]
    public string? Note { get; set; }

    public LocalDate TransactionDate { get; set; }

    public bool IsTransfer { get; set; }
    public Guid? TransferToAccountId { get; set; }

    [SensitiveData(Strategy = MaskingStrategy.Partial)]
    public string? Payee { get; set; }
    public string? Labels { get; set; }

    public string UserId { get; set; } = string.Empty;
}

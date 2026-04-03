using BudgetBuddy.API.VSA.Common.Domain.Contracts;
using BudgetBuddy.API.VSA.Common.Infrastructure.Logging;

namespace BudgetBuddy.API.VSA.Common.Domain.Entities;

public class Transaction : AuditableEntity, IUserOwnedEntity
{
    public Guid Id { get; set; }

    public Guid AccountId { get; set; }
    public Account Account { get; set; } = null!;

    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }

    public Guid? TypeId { get; set; }
    public CategoryType? Type { get; set; }

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

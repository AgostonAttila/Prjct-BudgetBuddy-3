using BudgetBuddy.Service.Transactions.Sagas;
using BudgetBuddy.Shared.Kernel.Contracts;
using BudgetBuddy.Shared.Kernel.Enums;
using BudgetBuddy.Shared.Kernel.Logging;
using NodaTime;

namespace BudgetBuddy.Service.Transactions.Domain;

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

    /// <summary>
    /// Saga compensation reversals are hidden from the user-facing transaction list.
    /// They still exist in the DB for audit purposes but should never appear in the UI.
    /// </summary>
    public bool IsHidden { get; set; }

    /// <summary>
    /// Tracks the choreography saga step for balance confirmation.
    /// Set to <see cref="TransactionSagaStep.Started"/> on creation; updated by
    /// <see cref="Messaging.Consumers.AccountBalanceSagaConsumer"/> on response, and by
    /// <c>SagaTimeoutJob</c> when no response arrives within the timeout window.
    /// </summary>
    public TransactionSagaStep SagaStep { get; set; } = TransactionSagaStep.Started;
}

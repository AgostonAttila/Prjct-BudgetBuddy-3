namespace BudgetBuddy.Service.Transactions.Sagas;

/// <summary>
/// Represents the current step of a distributed Transaction-Confirmation saga.
///
/// Flow (choreography — no central orchestrator):
///
///   [CreateTransaction] ──publishes──▶ TransactionsCreated
///                                              │
///                                    Accounts service consumes
///                                              │
///                              ┌───────────────┴────────────────┐
///                    balance OK│                                 │insufficient
///                              ▼                                 ▼
///             AccountsBalanceReserved              AccountsBalanceReservationFailed
///                              │                                 │
///                    Transactions service consumes               │
///                              │                                 │
///                              ▼                                 ▼
///                       Confirmed step                   Compensated step
///                    (transaction stands)          (transaction reversed via
///                                                  TransactionCompensationConsumer)
///
/// Item 9: Saga orchestration (choreography pattern)
/// </summary>
public enum TransactionSagaStep
{
    Started      = 0,
    Confirmed    = 1,
    Compensating = 2,
    Compensated  = 3,
    Failed       = 4,
}

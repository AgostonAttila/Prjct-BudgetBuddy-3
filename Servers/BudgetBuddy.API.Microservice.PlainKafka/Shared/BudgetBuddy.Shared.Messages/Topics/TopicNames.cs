namespace BudgetBuddy.Shared.Messages.Topics;

/// <summary>
/// Kafka topic name constants. All topics follow the convention:
/// <c>budgetbuddy.&lt;domain&gt;.&lt;event&gt;</c> (K-04).
///
/// DLQ naming — two levels, used in different contexts:
/// <list type="bullet">
///   <item><b>Domain DLQ constants</b> (<c>budgetbuddy.&lt;domain&gt;.dlq</c>): one dead-letter
///         queue per domain, configured as <c>DlqTopic</c> on consumers. Messages here are
///         permanently undeliverable and require manual intervention.</item>
///   <item><b><see cref="ToDlq"/></b> (<c>&lt;topic&gt;.dlq</c>): helper for tests and tooling
///         that need a dynamically derived DLQ name from any topic string.</item>
/// </list>
/// </summary>
public static class TopicNames
{
    // Accounts
    public const string AccountsChangelog    = "budgetbuddy.accounts.changelog";   // compacted
    public const string AccountsCreated      = "budgetbuddy.accounts.created";
    public const string AccountsUpdated      = "budgetbuddy.accounts.updated";
    public const string AccountsDeleted      = "budgetbuddy.accounts.deleted";

    // Transactions
    public const string TransactionsCreated  = "budgetbuddy.transactions.created";
    public const string TransactionsUpdated  = "budgetbuddy.transactions.updated";
    public const string TransactionsDeleted  = "budgetbuddy.transactions.deleted";

    // Budgets
    public const string BudgetsCreated       = "budgetbuddy.budgets.created";
    public const string BudgetsUpdated       = "budgetbuddy.budgets.updated";
    public const string BudgetsDeleted       = "budgetbuddy.budgets.deleted";
    public const string BudgetAlertTriggered = "budgetbuddy.budgets.alert-triggered";

    // Investments
    public const string InvestmentsCreated   = "budgetbuddy.investments.created";
    public const string InvestmentsUpdated   = "budgetbuddy.investments.updated";
    public const string InvestmentsDeleted   = "budgetbuddy.investments.deleted";
    public const string MarketPriceUpdated   = "budgetbuddy.investments.price-updated";

    // ReferenceData
    public const string CurrencySynced       = "budgetbuddy.referencedata.currency-synced";
    public const string CategoryChanged      = "budgetbuddy.referencedata.category-changed";

    // Users (from Keycloak webhook)
    public const string UserRegistered       = "budgetbuddy.users.registered";
    public const string UserDeleted          = "budgetbuddy.users.deleted";

    // Dead Letter Queues — üzenetek ide kerülnek MaxRetries után
    public const string TransactionsDlq      = "budgetbuddy.transactions.dlq";
    public const string AccountsDlq          = "budgetbuddy.accounts.dlq";
    public const string BudgetsDlq           = "budgetbuddy.budgets.dlq";
    public const string InvestmentsDlq       = "budgetbuddy.investments.dlq";
    public const string ReferenceDataDlq     = "budgetbuddy.referencedata.dlq";
    public const string UsersDlq             = "budgetbuddy.users.dlq";

    // Saga coordination — choreography compensation events
    public const string AccountsBalanceReserved         = "budgetbuddy.accounts.balance-reserved";
    public const string TransactionsReversed            = "budgetbuddy.transactions.reversed";

    /// <summary>Visszaadja az adott topic DLQ topic nevét (pl. "x.created" → "x.created.dlq").</summary>
    public static string ToDlq(string topic) => $"{topic}.dlq";
}

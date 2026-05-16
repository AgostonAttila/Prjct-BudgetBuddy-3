namespace BudgetBuddy.Shared.Messages.Topics;

public static class ConsumerGroups
{
    public const string AnalyticsTransactions   = "analytics-service-transactions-group";
    public const string AnalyticsAccounts       = "analytics-service-accounts-group";
    public const string AnalyticsBudgets        = "analytics-service-budgets-group";
    public const string AnalyticsInvestments    = "analytics-service-investments-group";
    public const string AnalyticsReferenceData  = "analytics-service-referencedata-group";
    public const string AnalyticsMarketPrice    = "analytics-service-market-price-group";

    public const string TransactionsAccounts      = "transactions-service-accounts-group";
    public const string TransactionsReferenceData = "transactions-service-referencedata-group";

    public const string BudgetsTransactions     = "budgets-service-transactions-group";
    public const string BudgetsReferenceData    = "budgets-service-referencedata-group";

    public const string AccountsTransactions    = "accounts-service-transactions-group";

    public const string InvestmentsAccounts     = "investments-service-accounts-group";

    public const string ReferenceDataAccounts   = "referencedata-service-accounts-group";

    public const string NotificationsBudgets    = "notifications-service-budgets-group";
    public const string NotificationsUsers      = "notifications-service-users-group";

    public const string TransactionsSaga        = "transactions-saga-consumer";
}

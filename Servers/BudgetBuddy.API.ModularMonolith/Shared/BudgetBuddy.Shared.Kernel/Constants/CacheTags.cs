namespace BudgetBuddy.Shared.Kernel.Constants;

/// <summary>
/// Cache tag constants for output cache invalidation
/// Used by ICacheInvalidator implementations
/// </summary>
public static class CacheTags
{
    // Accounts
    public const string AccountsList = "accounts-list";
    public const string AccountBalance = "account-balance";
    public const string PortfolioValue = "portfolio-value";

    // Transactions
    public const string Transactions = "transactions";

    // Dashboard
    public const string Dashboard = "dashboard";
    public const string MonthlySummary = "monthly-summary";
    public const string IncomeVsExpense = "income-vs-expense";
    public const string SpendingByCategory = "spending-by-category";

    // Budgets
    public const string BudgetsList = "budgets-list";
    public const string BudgetVsActual = "budget-vs-actual";
    public const string BudgetAlerts = "budget-alerts";

    // Categories
    public const string Categories = "categories";
    public const string CategoryTypes = "category-types";

    // Currencies
    public const string Currencies = "currencies";

    // Investments
    public const string Investments = "investments";
    public const string InvestmentPerformance = "investment-performance";
}

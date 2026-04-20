using System.Reflection;
using BudgetBuddy.Module.Accounts;
using BudgetBuddy.Module.Analytics;
using BudgetBuddy.Module.Auth;
using BudgetBuddy.Module.Budgets;
using BudgetBuddy.Module.Investments;
using BudgetBuddy.Module.ReferenceData;
using BudgetBuddy.Module.Transactions;
using BudgetBuddy.Shared.Contracts;
using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Kernel.Contracts;

namespace BudgetBuddy.ArchitectureTests;

internal static class Assemblies
{
    public static readonly Assembly Accounts        = typeof(AccountsModule).Assembly;
    public static readonly Assembly Analytics       = typeof(AnalyticsModule).Assembly;
    public static readonly Assembly Auth            = typeof(AuthModule).Assembly;
    public static readonly Assembly Budgets         = typeof(BudgetsModule).Assembly;
    public static readonly Assembly Investments     = typeof(InvestmentsModule).Assembly;
    public static readonly Assembly ReferenceData   = typeof(ReferenceDataModule).Assembly;
    public static readonly Assembly Transactions    = typeof(TransactionsModule).Assembly;

    public static readonly Assembly SharedKernel         = typeof(AuditableEntity).Assembly;
    public static readonly Assembly SharedContracts      = typeof(IModule).Assembly;
    public static readonly Assembly SharedInfrastructure = typeof(HttpContextCurrentUserService).Assembly;

    public static readonly Assembly[] AllModules =
    [
        Accounts, Analytics, Auth, Budgets, Investments, ReferenceData, Transactions
    ];

    public static readonly string[] AllModuleNamespaces =
    [
        "BudgetBuddy.Module.Accounts",
        "BudgetBuddy.Module.Analytics",
        "BudgetBuddy.Module.Auth",
        "BudgetBuddy.Module.Budgets",
        "BudgetBuddy.Module.Investments",
        "BudgetBuddy.Module.ReferenceData",
        "BudgetBuddy.Module.Transactions",
    ];
}

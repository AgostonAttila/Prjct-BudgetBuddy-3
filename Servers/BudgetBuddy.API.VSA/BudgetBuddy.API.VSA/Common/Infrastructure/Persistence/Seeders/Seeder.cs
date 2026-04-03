using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace BudgetBuddy.API.VSA.Common.Infrastructure.Persistence.Seeders;

internal class Seeder(AppDbContext dbContext, IClock clock, UserManager<User> userManager, ILogger<Seeder> logger) : ISeeder
{
    private readonly Instant _now = clock.GetCurrentInstant();
    private string? _demoUserId;

    public async Task Seed()
    {
        if (!await dbContext.Database.CanConnectAsync())
            return;

        // Get admin user ID to use for demo data
        var adminUser = await userManager.FindByEmailAsync("admin@budgetbuddy.com");
        if (adminUser == null)
        {
            logger.LogWarning("Admin user not found - skipping demo data seeding");
            return;
        }

        _demoUserId = adminUser.Id;
        logger.LogInformation("Seeding demo data for admin user: {Email}", adminUser.Email);

        await SeedCurrencies();
        await SeedCategories();
        await SeedCategoryTypes();
        await SeedAccounts();
        await SeedBudgets();
        await SeedTransactions();
        await SeedInvestments();
    }

    private async Task SeedCurrencies()
    {
        if (await dbContext.Currencies.AnyAsync())
            return;

        var currencies = GetCurrencies();
        dbContext.Currencies.AddRange(currencies);
        await dbContext.SaveChangesAsync();
    }

    private async Task SeedCategories()
    {
        if (await dbContext.Categories.AnyAsync())
            return;

        var categories = GetCategories();
        dbContext.Categories.AddRange(categories);
        await dbContext.SaveChangesAsync();
    }

    private async Task SeedCategoryTypes()
    {
        if (await dbContext.CategoryTypes.AnyAsync())
            return;

        var categoryTypes = GetCategoryTypes();
        dbContext.CategoryTypes.AddRange(categoryTypes);
        await dbContext.SaveChangesAsync();
    }

    private async Task SeedAccounts()
    {
        if (await dbContext.Accounts.AnyAsync())
            return;

        var accounts = GetAccounts();
        dbContext.Accounts.AddRange(accounts);
        await dbContext.SaveChangesAsync();
    }

    private async Task SeedBudgets()
    {
        if (await dbContext.Budgets.AnyAsync())
            return;

        var budgets = GetBudgets();
        dbContext.Budgets.AddRange(budgets);
        await dbContext.SaveChangesAsync();
    }

    private async Task SeedTransactions()
    {
        if (await dbContext.Transactions.AnyAsync())
            return;

        var transactions = GetTransactions();
        dbContext.Transactions.AddRange(transactions);
        await dbContext.SaveChangesAsync();
    }

    private async Task SeedInvestments()
    {
        if (await dbContext.Investments.AnyAsync())
            return;

        var investments = GetInvestments();
        dbContext.Investments.AddRange(investments);
        await dbContext.SaveChangesAsync();
    }

    #region Currency Data (Global)
    private List<Currency> GetCurrencies()
    {
        // Global currencies - shared across all users
        return
        [
            new Currency { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Code = "USD", Symbol = "$", Name = "US Dollar", CreatedAt = _now },
            new Currency { Id = Guid.Parse("11111111-1111-1111-1111-111111111112"), Code = "EUR", Symbol = "€", Name = "Euro", CreatedAt = _now },
            new Currency { Id = Guid.Parse("11111111-1111-1111-1111-111111111113"), Code = "HUF", Symbol = "Ft", Name = "Hungarian Forint", CreatedAt = _now },
            new Currency { Id = Guid.Parse("11111111-1111-1111-1111-111111111114"), Code = "GBP", Symbol = "£", Name = "British Pound", CreatedAt = _now },
            new Currency { Id = Guid.Parse("11111111-1111-1111-1111-111111111115"), Code = "JPY", Symbol = "¥", Name = "Japanese Yen", CreatedAt = _now },
            new Currency { Id = Guid.Parse("11111111-1111-1111-1111-111111111116"), Code = "CHF", Symbol = "Fr", Name = "Swiss Franc", CreatedAt = _now },
            new Currency { Id = Guid.Parse("11111111-1111-1111-1111-111111111117"), Code = "CAD", Symbol = "C$", Name = "Canadian Dollar", CreatedAt = _now },
            new Currency { Id = Guid.Parse("11111111-1111-1111-1111-111111111118"), Code = "AUD", Symbol = "A$", Name = "Australian Dollar", CreatedAt = _now },
            new Currency { Id = Guid.Parse("11111111-1111-1111-1111-111111111119"), Code = "CNY", Symbol = "¥", Name = "Chinese Yuan", CreatedAt = _now },
            new Currency { Id = Guid.Parse("11111111-1111-1111-1111-11111111111A"), Code = "SEK", Symbol = "kr", Name = "Swedish Krona", CreatedAt = _now },
            new Currency { Id = Guid.Parse("11111111-1111-1111-1111-11111111111B"), Code = "NOK", Symbol = "kr", Name = "Norwegian Krone", CreatedAt = _now },
            new Currency { Id = Guid.Parse("11111111-1111-1111-1111-11111111111C"), Code = "DKK", Symbol = "kr", Name = "Danish Krone", CreatedAt = _now },
            new Currency { Id = Guid.Parse("11111111-1111-1111-1111-11111111111D"), Code = "PLN", Symbol = "zł", Name = "Polish Zloty", CreatedAt = _now },
            new Currency { Id = Guid.Parse("11111111-1111-1111-1111-11111111111E"), Code = "CZK", Symbol = "Kč", Name = "Czech Koruna", CreatedAt = _now },
            new Currency { Id = Guid.Parse("11111111-1111-1111-1111-11111111111F"), Code = "RON", Symbol = "lei", Name = "Romanian Leu", CreatedAt = _now }
        ];
    }
    #endregion

    #region Category Data
    private List<Category> GetCategories()
    {
        return
        [
            // Income categories
            new Category
            {
                Id = CategoryIds.Salary,
                UserId = _demoUserId!,
                Name = "Salary",
                Icon = "💰",
                Color = "#4CAF50",
                CreatedAt = _now
            },
            new Category
            {
                Id = CategoryIds.Freelance,
                UserId = _demoUserId!,
                Name = "Freelance",
                Icon = "💼",
                Color = "#8BC34A",
                CreatedAt = _now
            },
            new Category
            {
                Id = CategoryIds.Investment,
                UserId = _demoUserId!,
                Name = "Investment Income",
                Icon = "📈",
                Color = "#009688",
                CreatedAt = _now
            },

            // Expense categories
            new Category
            {
                Id = CategoryIds.Groceries,
                UserId = _demoUserId!,
                Name = "Groceries",
                Icon = "🛒",
                Color = "#FF9800",
                CreatedAt = _now
            },
            new Category
            {
                Id = CategoryIds.Transportation,
                UserId = _demoUserId!,
                Name = "Transportation",
                Icon = "🚗",
                Color = "#2196F3",
                CreatedAt = _now
            },
            new Category
            {
                Id = CategoryIds.Entertainment,
                UserId = _demoUserId!,
                Name = "Entertainment",
                Icon = "🎬",
                Color = "#9C27B0",
                CreatedAt = _now
            },
            new Category
            {
                Id = CategoryIds.Bills,
                UserId = _demoUserId!,
                Name = "Bills & Utilities",
                Icon = "💡",
                Color = "#F44336",
                CreatedAt = _now
            },
            new Category
            {
                Id = CategoryIds.DiningOut,
                UserId = _demoUserId!,
                Name = "Dining Out",
                Icon = "🍔",
                Color = "#FF5722",
                CreatedAt = _now
            },
            new Category
            {
                Id = CategoryIds.Shopping,
                UserId = _demoUserId!,
                Name = "Shopping",
                Icon = "🛍️",
                Color = "#E91E63",
                CreatedAt = _now
            },
            new Category
            {
                Id = CategoryIds.Healthcare,
                UserId = _demoUserId!,
                Name = "Healthcare",
                Icon = "🏥",
                Color = "#F44336",
                CreatedAt = _now
            }
        ];
    }

    private List<CategoryType> GetCategoryTypes()
    {
        return
        [
            // Groceries subtypes
            new CategoryType
            {
                Id = Guid.NewGuid(),
                CategoryId = CategoryIds.Groceries,
                Name = "Supermarket",
                Icon = "🏪",
                Color = "#FF9800",
                CreatedAt = _now
            },
            new CategoryType
            {
                Id = Guid.NewGuid(),
                CategoryId = CategoryIds.Groceries,
                Name = "Farmer's Market",
                Icon = "🥕",
                Color = "#FFA726",
                CreatedAt = _now
            },

            // Transportation subtypes
            new CategoryType
            {
                Id = Guid.NewGuid(),
                CategoryId = CategoryIds.Transportation,
                Name = "Public Transport",
                Icon = "🚇",
                Color = "#2196F3",
                CreatedAt = _now
            },
            new CategoryType
            {
                Id = Guid.NewGuid(),
                CategoryId = CategoryIds.Transportation,
                Name = "Fuel",
                Icon = "⛽",
                Color = "#1976D2",
                CreatedAt = _now
            },
            new CategoryType
            {
                Id = Guid.NewGuid(),
                CategoryId = CategoryIds.Transportation,
                Name = "Parking",
                Icon = "🅿️",
                Color = "#1565C0",
                CreatedAt = _now
            },

            // Entertainment subtypes
            new CategoryType
            {
                Id = Guid.NewGuid(),
                CategoryId = CategoryIds.Entertainment,
                Name = "Movies",
                Icon = "🎥",
                Color = "#9C27B0",
                CreatedAt = _now
            },
            new CategoryType
            {
                Id = Guid.NewGuid(),
                CategoryId = CategoryIds.Entertainment,
                Name = "Concerts",
                Icon = "🎵",
                Color = "#7B1FA2",
                CreatedAt = _now
            },
            new CategoryType
            {
                Id = Guid.NewGuid(),
                CategoryId = CategoryIds.Entertainment,
                Name = "Gaming",
                Icon = "🎮",
                Color = "#6A1B9A",
                CreatedAt = _now
            },

            // Bills subtypes
            new CategoryType
            {
                Id = Guid.NewGuid(),
                CategoryId = CategoryIds.Bills,
                Name = "Electricity",
                Icon = "⚡",
                Color = "#F44336",
                CreatedAt = _now
            },
            new CategoryType
            {
                Id = Guid.NewGuid(),
                CategoryId = CategoryIds.Bills,
                Name = "Internet",
                Icon = "🌐",
                Color = "#E53935",
                CreatedAt = _now
            },
            new CategoryType
            {
                Id = Guid.NewGuid(),
                CategoryId = CategoryIds.Bills,
                Name = "Phone",
                Icon = "📱",
                Color = "#D32F2F",
                CreatedAt = _now
            }
        ];
    }
    #endregion

    #region Account Data
    private List<Account> GetAccounts()
    {
        return
        [
            new Account
            {
                Id = AccountIds.Checking,
                UserId = _demoUserId!,
                Name = "Main Checking Account",
                Description = "Primary checking account for daily expenses",
                DefaultCurrencyCode = "USD",
                InitialBalance = 5000.00m,
                CreatedAt = _now.Minus(Duration.FromDays(365))
            },
            new Account
            {
                Id = AccountIds.Savings,
                UserId = _demoUserId!,
                Name = "Savings Account",
                Description = "Emergency fund and savings",
                DefaultCurrencyCode = "USD",
                InitialBalance = 15000.00m,
                CreatedAt = _now.Minus(Duration.FromDays(365))
            },
            new Account
            {
                Id = AccountIds.Cash,
                UserId = _demoUserId!,
                Name = "Cash Wallet",
                Description = "Physical cash on hand",
                DefaultCurrencyCode = "USD",
                InitialBalance = 500.00m,
                CreatedAt = _now.Minus(Duration.FromDays(365))
            },
            new Account
            {
                Id = AccountIds.CreditCard,
                UserId = _demoUserId!,
                Name = "Credit Card",
                Description = "Visa Credit Card ending in 4567",
                DefaultCurrencyCode = "USD",
                InitialBalance = -1200.00m, // Negative balance for credit card debt
                CreatedAt = _now.Minus(Duration.FromDays(365))
            }
        ];
    }
    #endregion

    #region Budget Data
    private List<Budget> GetBudgets()
    {
        var currentMonth = _now.InUtc().Date;

        return
        [
            new Budget
            {
                Id = Guid.NewGuid(),
                UserId = _demoUserId!,
                CategoryId = CategoryIds.Groceries,
                Name = "Monthly Groceries Budget",
                Amount = 600.00m,
                CurrencyCode = "USD",
                Year = currentMonth.Year,
                Month = currentMonth.Month,
                CreatedAt = _now
            },
            new Budget
            {
                Id = Guid.NewGuid(),
                UserId = _demoUserId!,
                CategoryId = CategoryIds.Transportation,
                Name = "Monthly Transportation Budget",
                Amount = 300.00m,
                CurrencyCode = "USD",
                Year = currentMonth.Year,
                Month = currentMonth.Month,
                CreatedAt = _now
            },
            new Budget
            {
                Id = Guid.NewGuid(),
                UserId = _demoUserId!,
                CategoryId = CategoryIds.Entertainment,
                Name = "Monthly Entertainment Budget",
                Amount = 200.00m,
                CurrencyCode = "USD",
                Year = currentMonth.Year,
                Month = currentMonth.Month,
                CreatedAt = _now
            },
            new Budget
            {
                Id = Guid.NewGuid(),
                UserId = _demoUserId!,
                CategoryId = CategoryIds.DiningOut,
                Name = "Monthly Dining Out Budget",
                Amount = 400.00m,
                CurrencyCode = "USD",
                Year = currentMonth.Year,
                Month = currentMonth.Month,
                CreatedAt = _now
            },
            new Budget
            {
                Id = Guid.NewGuid(),
                UserId = _demoUserId!,
                CategoryId = CategoryIds.Bills,
                Name = "Monthly Bills Budget",
                Amount = 800.00m,
                CurrencyCode = "USD",
                Year = currentMonth.Year,
                Month = currentMonth.Month,
                CreatedAt = _now
            }
        ];
    }
    #endregion

    #region Transaction Data
    private List<Transaction> GetTransactions()
    {
        var today = _now.InUtc().Date;

        return
        [
            // Income transactions
            new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = _demoUserId!,
                AccountId = AccountIds.Checking,
                CategoryId = CategoryIds.Salary,
                Amount = 5000.00m,
                CurrencyCode = "USD",
                TransactionType = TransactionType.Income,
                PaymentType = PaymentType.BankTransfer,
                TransactionDate = today.Minus(Period.FromDays(5)),
                Note = "Monthly salary payment",
                Payee = "Acme Corporation",
                IsTransfer = false,
                CreatedAt = _now.Minus(Duration.FromDays(5))
            },
            new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = _demoUserId!,
                AccountId = AccountIds.Checking,
                CategoryId = CategoryIds.Freelance,
                Amount = 1200.00m,
                CurrencyCode = "USD",
                TransactionType = TransactionType.Income,
                PaymentType = PaymentType.BankTransfer,
                TransactionDate = today.Minus(Period.FromDays(10)),
                Note = "Website development project",
                Payee = "Tech Startup Inc",
                IsTransfer = false,
                CreatedAt = _now.Minus(Duration.FromDays(10))
            },

            // Expense transactions - Groceries
            new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = _demoUserId!,
                AccountId = AccountIds.Checking,
                CategoryId = CategoryIds.Groceries,
                Amount = 87.50m,
                CurrencyCode = "USD",
                TransactionType = TransactionType.Expense,
                PaymentType = PaymentType.Card,
                TransactionDate = today.Minus(Period.FromDays(2)),
                Note = "Weekly grocery shopping",
                Payee = "Whole Foods Market",
                IsTransfer = false,
                CreatedAt = _now.Minus(Duration.FromDays(2))
            },
            new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = _demoUserId!,
                AccountId = AccountIds.Checking,
                CategoryId = CategoryIds.Groceries,
                Amount = 54.30m,
                CurrencyCode = "USD",
                TransactionType = TransactionType.Expense,
                PaymentType = PaymentType.Card,
                TransactionDate = today.Minus(Period.FromDays(7)),
                Note = "Fresh produce and dairy",
                Payee = "Trader Joe's",
                IsTransfer = false,
                CreatedAt = _now.Minus(Duration.FromDays(7))
            },

            // Transportation
            new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = _demoUserId!,
                AccountId = AccountIds.Checking,
                CategoryId = CategoryIds.Transportation,
                Amount = 45.00m,
                CurrencyCode = "USD",
                TransactionType = TransactionType.Expense,
                PaymentType = PaymentType.Card,
                TransactionDate = today.Minus(Period.FromDays(3)),
                Note = "Gas refill",
                Payee = "Shell Gas Station",
                IsTransfer = false,
                CreatedAt = _now.Minus(Duration.FromDays(3))
            },
            new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = _demoUserId!,
                AccountId = AccountIds.Cash,
                CategoryId = CategoryIds.Transportation,
                Amount = 2.75m,
                CurrencyCode = "USD",
                TransactionType = TransactionType.Expense,
                PaymentType = PaymentType.Cash,
                TransactionDate = today.Minus(Period.FromDays(1)),
                Note = "Subway fare",
                Payee = "MTA",
                IsTransfer = false,
                CreatedAt = _now.Minus(Duration.FromDays(1))
            },

            // Entertainment
            new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = _demoUserId!,
                AccountId = AccountIds.CreditCard,
                CategoryId = CategoryIds.Entertainment,
                Amount = 15.99m,
                CurrencyCode = "USD",
                TransactionType = TransactionType.Expense,
                PaymentType = PaymentType.Card,
                TransactionDate = today.Minus(Period.FromDays(1)),
                Note = "Monthly streaming subscription",
                Payee = "Netflix",
                Labels = "subscription,recurring",
                IsTransfer = false,
                CreatedAt = _now.Minus(Duration.FromDays(1))
            },
            new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = _demoUserId!,
                AccountId = AccountIds.Checking,
                CategoryId = CategoryIds.Entertainment,
                Amount = 45.00m,
                CurrencyCode = "USD",
                TransactionType = TransactionType.Expense,
                PaymentType = PaymentType.Card,
                TransactionDate = today.Minus(Period.FromDays(8)),
                Note = "Movie tickets and popcorn",
                Payee = "AMC Theaters",
                IsTransfer = false,
                CreatedAt = _now.Minus(Duration.FromDays(8))
            },

            // Dining Out
            new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = _demoUserId!,
                AccountId = AccountIds.Checking,
                CategoryId = CategoryIds.DiningOut,
                Amount = 65.00m,
                CurrencyCode = "USD",
                TransactionType = TransactionType.Expense,
                PaymentType = PaymentType.Card,
                TransactionDate = today.Minus(Period.FromDays(4)),
                Note = "Dinner with friends",
                Payee = "Italian Restaurant",
                IsTransfer = false,
                CreatedAt = _now.Minus(Duration.FromDays(4))
            },
            new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = _demoUserId!,
                AccountId = AccountIds.Cash,
                CategoryId = CategoryIds.DiningOut,
                Amount = 12.50m,
                CurrencyCode = "USD",
                TransactionType = TransactionType.Expense,
                PaymentType = PaymentType.Cash,
                TransactionDate = today,
                Note = "Coffee and breakfast",
                Payee = "Starbucks",
                IsTransfer = false,
                CreatedAt = _now
            },

            // Bills
            new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = _demoUserId!,
                AccountId = AccountIds.Checking,
                CategoryId = CategoryIds.Bills,
                Amount = 120.00m,
                CurrencyCode = "USD",
                TransactionType = TransactionType.Expense,
                PaymentType = PaymentType.BankTransfer,
                TransactionDate = today.Minus(Period.FromDays(15)),
                Note = "Monthly electricity bill",
                Payee = "Power Company",
                Labels = "utility,recurring",
                IsTransfer = false,
                CreatedAt = _now.Minus(Duration.FromDays(15))
            },
            new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = _demoUserId!,
                AccountId = AccountIds.Checking,
                CategoryId = CategoryIds.Bills,
                Amount = 79.99m,
                CurrencyCode = "USD",
                TransactionType = TransactionType.Expense,
                PaymentType = PaymentType.BankTransfer,
                TransactionDate = today.Minus(Period.FromDays(12)),
                Note = "Internet and cable",
                Payee = "ISP Provider",
                Labels = "utility,recurring",
                IsTransfer = false,
                CreatedAt = _now.Minus(Duration.FromDays(12))
            },

            // Transfer between accounts
            new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = _demoUserId!,
                AccountId = AccountIds.Checking,
                Amount = 1000.00m,
                CurrencyCode = "USD",
                TransactionType = TransactionType.Transfer,
                PaymentType = PaymentType.BankTransfer,
                TransactionDate = today.Minus(Period.FromDays(20)),
                Note = "Transfer to savings",
                IsTransfer = true,
                TransferToAccountId = AccountIds.Savings,
                CreatedAt = _now.Minus(Duration.FromDays(20))
            }
        ];
    }
    #endregion

    #region Investment Data
    private List<Investment> GetInvestments()
    {
        var today = _now.InUtc().Date;

        return
        [
            new Investment
            {
                Id = Guid.NewGuid(),
                UserId = _demoUserId!,
                AccountId = AccountIds.Savings,
                Symbol = "AAPL",
                Name = "Apple Inc.",
                Type = InvestmentType.Stock,
                Quantity = 10m,
                PurchasePrice = 150.00m,
                CurrencyCode = "USD",
                PurchaseDate = today.Minus(Period.FromMonths(6)),
                Note = "Tech stock investment",
                CreatedAt = _now.Minus(Duration.FromDays(180))
            },
            new Investment
            {
                Id = Guid.NewGuid(),
                UserId = _demoUserId!,
                AccountId = AccountIds.Savings,
                Symbol = "VOO",
                Name = "Vanguard S&P 500 ETF",
                Type = InvestmentType.Etf,
                Quantity = 25m,
                PurchasePrice = 400.00m,
                CurrencyCode = "USD",
                PurchaseDate = today.Minus(Period.FromMonths(12)),
                Note = "Long-term index fund",
                CreatedAt = _now.Minus(Duration.FromDays(365))
            },
            new Investment
            {
                Id = Guid.NewGuid(),
                UserId = _demoUserId!,
                Symbol = "BTC",
                Name = "Bitcoin",
                Type = InvestmentType.Crypto,
                Quantity = 0.5m,
                PurchasePrice = 45000.00m,
                CurrencyCode = "USD",
                PurchaseDate = today.Minus(Period.FromMonths(8)),
                Note = "Cryptocurrency investment",
                CreatedAt = _now.Minus(Duration.FromDays(240))
            },
            new Investment
            {
                Id = Guid.NewGuid(),
                UserId = _demoUserId!,
                AccountId = AccountIds.Savings,
                Symbol = "MSFT",
                Name = "Microsoft Corporation",
                Type = InvestmentType.Stock,
                Quantity = 15m,
                PurchasePrice = 330.00m,
                CurrencyCode = "USD",
                PurchaseDate = today.Minus(Period.FromMonths(4)),
                Note = "Tech stock diversification",
                CreatedAt = _now.Minus(Duration.FromDays(120))
            },
            new Investment
            {
                Id = Guid.NewGuid(),
                UserId = _demoUserId!,
                AccountId = AccountIds.Savings,
                Symbol = "VTI",
                Name = "Vanguard Total Stock Market ETF",
                Type = InvestmentType.Etf,
                Quantity = 30m,
                PurchasePrice = 220.00m,
                CurrencyCode = "USD",
                PurchaseDate = today.Minus(Period.FromMonths(10)),
                Note = "Broad market exposure",
                CreatedAt = _now.Minus(Duration.FromDays(300))
            }
        ];
    }
    #endregion

    #region Helper ID Classes
    private static class CategoryIds
    {
        public static readonly Guid Salary = Guid.Parse("22222222-2222-2222-2222-222222222201");
        public static readonly Guid Freelance = Guid.Parse("22222222-2222-2222-2222-222222222202");
        public static readonly Guid Investment = Guid.Parse("22222222-2222-2222-2222-222222222203");
        public static readonly Guid Groceries = Guid.Parse("22222222-2222-2222-2222-222222222204");
        public static readonly Guid Transportation = Guid.Parse("22222222-2222-2222-2222-222222222205");
        public static readonly Guid Entertainment = Guid.Parse("22222222-2222-2222-2222-222222222206");
        public static readonly Guid Bills = Guid.Parse("22222222-2222-2222-2222-222222222207");
        public static readonly Guid DiningOut = Guid.Parse("22222222-2222-2222-2222-222222222208");
        public static readonly Guid Shopping = Guid.Parse("22222222-2222-2222-2222-222222222209");
        public static readonly Guid Healthcare = Guid.Parse("22222222-2222-2222-2222-222222222210");
    }

    private static class AccountIds
    {
        public static readonly Guid Checking = Guid.Parse("33333333-3333-3333-3333-333333333301");
        public static readonly Guid Savings = Guid.Parse("33333333-3333-3333-3333-333333333302");
        public static readonly Guid Cash = Guid.Parse("33333333-3333-3333-3333-333333333303");
        public static readonly Guid CreditCard = Guid.Parse("33333333-3333-3333-3333-333333333304");
    }
    #endregion
}
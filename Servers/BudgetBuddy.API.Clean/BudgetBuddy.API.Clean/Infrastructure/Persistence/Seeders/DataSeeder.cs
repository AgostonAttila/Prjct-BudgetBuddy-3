namespace BudgetBuddy.Infrastructure.Services;

public class DataSeeder(AppDbContext context, IClock clock, ILogger<DataSeeder> logger)
{
    public async Task SeedDefaultDataForUserAsync(string userId)
    {
        try
        {
            // Check if user already has data
            var hasCategories = await context.Categories.AnyAsync(c => c.UserId == userId);

            if (hasCategories)
            {
                logger.LogInformation("User {UserId} already has seed data", userId);
                return;
            }

            var now = clock.GetCurrentInstant();

            // Seed default categories if none exist
            if (!hasCategories)
            {
                var defaultCategories = new List<Category>
                {
                    new() { Id = Guid.NewGuid(), UserId = userId, Name = "Groceries", Icon = "🛒", Color = "#4CAF50", CreatedAt = now },
                    new() { Id = Guid.NewGuid(), UserId = userId, Name = "Transportation", Icon = "🚗", Color = "#2196F3", CreatedAt = now },
                    new() { Id = Guid.NewGuid(), UserId = userId, Name = "Entertainment", Icon = "🎬", Color = "#9C27B0", CreatedAt = now },
                    new() { Id = Guid.NewGuid(), UserId = userId, Name = "Bills & Utilities", Icon = "💡", Color = "#FF9800", CreatedAt = now },
                    new() { Id = Guid.NewGuid(), UserId = userId, Name = "Healthcare", Icon = "🏥", Color = "#F44336", CreatedAt = now },
                    new() { Id = Guid.NewGuid(), UserId = userId, Name = "Shopping", Icon = "🛍️", Color = "#E91E63", CreatedAt = now },
                    new() { Id = Guid.NewGuid(), UserId = userId, Name = "Dining Out", Icon = "🍔", Color = "#FF5722", CreatedAt = now },
                    new() { Id = Guid.NewGuid(), UserId = userId, Name = "Travel", Icon = "✈️", Color = "#00BCD4", CreatedAt = now },
                    new() { Id = Guid.NewGuid(), UserId = userId, Name = "Education", Icon = "📚", Color = "#3F51B5", CreatedAt = now },
                    new() { Id = Guid.NewGuid(), UserId = userId, Name = "Salary", Icon = "💰", Color = "#4CAF50", CreatedAt = now },
                    new() { Id = Guid.NewGuid(), UserId = userId, Name = "Investment Income", Icon = "📈", Color = "#009688", CreatedAt = now },
                    new() { Id = Guid.NewGuid(), UserId = userId, Name = "Other Income", Icon = "💵", Color = "#8BC34A", CreatedAt = now }
                };

                await context.Categories.AddRangeAsync(defaultCategories);
                logger.LogInformation("Seeded {Count} default categories for user {UserId}", defaultCategories.Count, userId);
            }

            // Note: Currencies are now global and seeded via migration - no need to seed per user

            await context.SaveChangesAsync();
            logger.LogInformation("Successfully seeded default data for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error seeding default data for user {UserId}", userId);
            throw;
        }
    }
}

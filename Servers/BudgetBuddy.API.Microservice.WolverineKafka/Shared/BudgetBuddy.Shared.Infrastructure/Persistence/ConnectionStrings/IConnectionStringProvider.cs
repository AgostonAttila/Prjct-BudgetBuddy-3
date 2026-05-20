namespace BudgetBuddy.Shared.Infrastructure.Persistence.ConnectionStrings;

public interface IConnectionStringProvider
{
    string GetDbConnectionString();
}
namespace BudgetBuddy.Infrastructure.Persistence.ConnectionStrings;

public interface IConnectionStringProvider
{
    string GetDbConnectionString();
}
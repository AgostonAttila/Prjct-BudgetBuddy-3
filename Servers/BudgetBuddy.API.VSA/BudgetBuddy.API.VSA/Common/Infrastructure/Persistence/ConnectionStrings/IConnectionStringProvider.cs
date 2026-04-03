namespace BudgetBuddy.API.VSA.Common.Infrastructure.Persistence.ConnectionStrings;

public interface IConnectionStringProvider
{
    string GetDbConnectionString();
}
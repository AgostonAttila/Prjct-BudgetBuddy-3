namespace BudgetBuddy.API.VSA.Common.Domain.Contracts;

/// <summary>
/// Marker interface for entities that belong to a specific user
/// </summary>
public interface IUserOwned
{
    string UserId { get; set; }
}

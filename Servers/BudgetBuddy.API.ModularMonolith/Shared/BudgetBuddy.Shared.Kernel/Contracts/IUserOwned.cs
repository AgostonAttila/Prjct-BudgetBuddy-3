namespace BudgetBuddy.Shared.Kernel.Contracts;

/// <summary>
/// Marker interface for entities that belong to a specific user
/// </summary>
public interface IUserOwned
{
    string UserId { get; set; }
}

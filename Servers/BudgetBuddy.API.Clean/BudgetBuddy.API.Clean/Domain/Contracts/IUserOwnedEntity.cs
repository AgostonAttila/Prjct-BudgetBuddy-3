namespace BudgetBuddy.Domain.Contracts;

/// <summary>
/// Entity with both a GUID primary key and a UserId ownership claim.
/// </summary>
public interface IUserOwnedEntity : IUserOwned
{
    Guid Id { get; }
}

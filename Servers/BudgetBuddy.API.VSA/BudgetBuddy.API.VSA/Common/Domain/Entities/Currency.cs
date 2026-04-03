using BudgetBuddy.API.VSA.Common.Domain.Contracts;

namespace BudgetBuddy.API.VSA.Common.Domain.Entities;

/// <summary>
/// Global currency entity - shared across all users
/// </summary>
public class Currency : AuditableEntity
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty; // USD, EUR, HUF
    public string Symbol { get; set; } = string.Empty; // $, €, Ft
    public string Name { get; set; } = string.Empty;
}

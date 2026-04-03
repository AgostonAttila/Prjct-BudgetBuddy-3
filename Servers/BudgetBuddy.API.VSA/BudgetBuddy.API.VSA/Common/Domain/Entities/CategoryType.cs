using System.Text.Json.Serialization;
using BudgetBuddy.API.VSA.Common.Domain.Contracts;


namespace BudgetBuddy.API.VSA.Common.Domain.Entities;

public class CategoryType : AuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Color { get; set; }

    [JsonIgnore]
    public Guid CategoryId { get; set; }
    [JsonIgnore]
    public Category Category { get; set; } = null!;

    public ICollection<Transaction> Transactions { get; set; } = [];

}

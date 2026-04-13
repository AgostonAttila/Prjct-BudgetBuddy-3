using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

internal static class ArchitectureTestBase
{
    internal static readonly Architecture Architecture =
        new ArchLoader()
            .LoadAssemblies(
                typeof(BudgetBuddy.Domain.Entities.Account).Assembly,
                typeof(BudgetBuddy.Application.Extensions.ApplicationExtensions).Assembly,
                typeof(BudgetBuddy.Infrastructure.Extensions.InfrastructureServicesExtensions).Assembly,
                typeof(BudgetBuddy.API.Endpoints.AccountModule).Assembly
            )
            .Build();

    internal static readonly IObjectProvider<IType> DomainLayer =
        Types().That().ResideInNamespaceMatching("BudgetBuddy\\.Domain.*").As("Domain Layer");

    internal static readonly IObjectProvider<IType> ApplicationLayer =
        Types().That().ResideInNamespaceMatching("BudgetBuddy\\.Application.*").As("Application Layer");

    internal static readonly IObjectProvider<IType> InfrastructureLayer =
        Types().That().ResideInNamespaceMatching("BudgetBuddy\\.Infrastructure.*").As("Infrastructure Layer");

    internal static readonly IObjectProvider<IType> ApiLayer =
        Types().That().ResideInNamespaceMatching("BudgetBuddy\\.API.*").As("API Layer");
}

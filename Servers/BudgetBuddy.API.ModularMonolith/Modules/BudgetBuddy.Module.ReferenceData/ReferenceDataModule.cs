using BudgetBuddy.Shared.Contracts;
using BudgetBuddy.Shared.Contracts.ReferenceData;
using BudgetBuddy.Module.ReferenceData.Features.Categories.Services;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetBuddy.Module.ReferenceData;

public class ReferenceDataModule : IModule
{
    public string ModuleName => "ReferenceData";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<ReferenceDataDbContext>(configuration);
        services.AddScoped<ICategoryQueryService, CategoryQueryService>();

        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}

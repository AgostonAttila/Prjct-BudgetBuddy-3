using BudgetBuddy.Shared.Contracts;
using BudgetBuddy.Shared.Contracts.Budgets;
using BudgetBuddy.Module.Budgets.Features.BudgetAlerts.Services;
using BudgetBuddy.Module.Budgets.Features.Budgets.Services;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetBuddy.Module.Budgets;

public class BudgetsModule : IModule
{
    public string ModuleName => "Budgets";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<BudgetsDbContext>(configuration);
        services.AddScoped<BudgetAlertService>();
        services.AddScoped<IBudgetAlertService>(sp => sp.GetRequiredService<BudgetAlertService>());
        services.AddScoped<IBudgetAlertCalculationService>(sp => sp.GetRequiredService<BudgetAlertService>());
        services.AddScoped<IBudgetAlertRuleEngine>(sp => sp.GetRequiredService<BudgetAlertService>());
        services.AddScoped<IBudgetAlertMessageFormatter>(sp => sp.GetRequiredService<BudgetAlertService>());
        services.AddScoped<IBudgetQueryService, BudgetQueryService>();

        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}

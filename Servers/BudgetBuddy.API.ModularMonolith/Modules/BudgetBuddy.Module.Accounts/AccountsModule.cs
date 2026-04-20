using BudgetBuddy.Shared.Contracts;
using BudgetBuddy.Shared.Contracts.Accounts;
using BudgetBuddy.Module.Accounts.Services;
using BudgetBuddy.Module.Accounts.Persistence;
using BudgetBuddy.Shared.Infrastructure.Extensions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetBuddy.Module.Accounts;

public class AccountsModule : IModule
{
    public string ModuleName => "Accounts";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<AccountsDbContext>(configuration);
        services.AddScoped<IAccountBalanceService, AccountBalanceService>();
        services.AddScoped<IAccountOwnershipService, AccountOwnershipService>();

        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}

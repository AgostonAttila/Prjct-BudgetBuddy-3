using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;

namespace BudgetBuddy.Shared.Contracts;

public interface IModule
{
    string ModuleName { get; }
    IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration);
    IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints);
}

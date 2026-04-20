using BudgetBuddy.Shared.Contracts;
using BudgetBuddy.Module.Analytics.Features.Dashboard.Services;
using BudgetBuddy.Module.Analytics.Features.Reports.Services;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetBuddy.Module.Analytics;

public class AnalyticsModule : IModule
{
    public string ModuleName => "Analytics";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IDashboardService, DashboardService>();

        services.AddScoped<ReportService>();
        services.AddScoped<IReportService>(sp => sp.GetRequiredService<ReportService>());
        services.AddScoped<IIncomeExpenseReportService>(sp => sp.GetRequiredService<ReportService>());
        services.AddScoped<IMonthlySummaryReportService>(sp => sp.GetRequiredService<ReportService>());
        services.AddScoped<ISpendingReportService>(sp => sp.GetRequiredService<ReportService>());
        services.AddScoped<IInvestmentReportService>(sp => sp.GetRequiredService<ReportService>());

        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}

using BudgetBuddy.Shared.Infrastructure.DataExchange;
using BudgetBuddy.Shared.Infrastructure.Filters;
using BudgetBuddy.Shared.Infrastructure.Middlewares;
using BudgetBuddy.Shared.Infrastructure.Notification.Email;
using BudgetBuddy.Shared.Infrastructure.Security.Encryption;
using BudgetBuddy.Shared.Infrastructure.Security.Filescanning;
using BudgetBuddy.Shared.Infrastructure.Services;

namespace BudgetBuddy.Shared.Infrastructure.Extensions;

public static class DependencyInjectionExtensions
{
    public static void AddApplicationServices(this IServiceCollection services)
    {
        // Security Services
        services.AddSingleton<IDataProtectionService, DataProtectionService>();
        services.AddSingleton<IEncryptionService, EncryptionService>();
        services.AddSingleton<IAntivirusService, ClamAVService>();

        // A-11: ClamAV health check — "infrastructure" tag keeps it out of /health/ready
        // so a slow ClamAV daemon startup doesn't block Kubernetes readiness probes
        services.AddHealthChecks()
            .AddCheck<ClamAVHealthCheck>("clamav", tags: ["infrastructure"]);

        // Export Services
        services.AddScoped<ICsvExportService, CsvExportService>();
        services.AddScoped<IExcelExportService, ExcelExportService>();
        services.AddScoped<IExportFactory, ExportFactory>();

        // Shared Services
        services.AddScoped<IBatchDeleteService, BatchDeleteService>();
        services.AddScoped<IUserCacheInvalidator, UserCacheInvalidator>();

        // Email
        services.AddScoped<IEmailService, EmailService>();

        // Filters & Middlewares
        services.AddScoped<IdempotencyFilter>();
        services.AddScoped<RequestTimeLoggingMiddleware>();
    }
}

using BudgetBuddy.Shared.Infrastructure.Filters;
using BudgetBuddy.Shared.Infrastructure.Security;
using BudgetBuddy.Shared.Infrastructure.Services;
using BudgetBuddy.Shared.Infrastructure.Middlewares;
using BudgetBuddy.Shared.Infrastructure.DataExchange;
using BudgetBuddy.Shared.Infrastructure.Notification.Email;
using BudgetBuddy.Shared.Infrastructure.Security.Encryption;
using BudgetBuddy.Shared.Infrastructure.Security.Filescanning;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace BudgetBuddy.Shared.Infrastructure.Extensions;

public static class DependencyInjectionExtensions
{
    public static void AddApplicationServices(this IServiceCollection services)
    {
        // Security Services
        services.AddSingleton<IDataProtectionService, DataProtectionService>();
        services.AddSingleton<IEncryptionService, EncryptionService>();
        services.AddSingleton<IAntivirusService, ClamAVService>();

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

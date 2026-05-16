using Asp.Versioning;
using BudgetBuddy.Shared.Infrastructure.Behaviors;
using BudgetBuddy.Shared.Infrastructure.Exceptions;
using BudgetBuddy.Shared.Infrastructure.Services;
using Carter;
using FluentValidation;
using Mapster;
using Microsoft.FeatureManagement;
using NodaTime.Serialization.SystemTextJson;
using Scalar.AspNetCore;

namespace BudgetBuddy.Shared.Infrastructure.Extensions;

public static class ApiExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration,
        params System.Reflection.Assembly[] serviceAssemblies)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, HttpContextCurrentUserService>();

        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
            options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        });
        services.AddSingleton<IClock>(SystemClock.Instance);

        var allAssemblies = new[]
        {
            typeof(ApiExtensions).Assembly,
        }.Concat(serviceAssemblies).ToArray();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(allAssemblies));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(MetricsBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CacheInvalidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(BulkheadBehavior<,>));

        foreach (var assembly in allAssemblies)
        {
            services.AddValidatorsFromAssembly(assembly);
        }

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddApiVersioning(opt =>
        {
            opt.DefaultApiVersion = new ApiVersion(1, 0);
            opt.AssumeDefaultVersionWhenUnspecified = true;
            opt.ReportApiVersions = true;
        });

        services.AddCarter(configurator: c =>
        {
            foreach (var assembly in allAssemblies)
            {
                c.WithModules(assembly.GetTypes()
                    .Where(t => t.IsAssignableTo(typeof(ICarterModule)) && !t.IsAbstract)
                    .ToArray());
            }
        });

        var mapsterConfig = TypeAdapterConfig.GlobalSettings;
        foreach (var assembly in allAssemblies)
        {
            mapsterConfig.Scan(assembly);
        }

        services.AddSingleton(mapsterConfig);
        services.AddMapster();

        services.AddOpenApi();

        // A-31: Feature Management — replaces raw GetValue<bool>("Features:X") config checks.
        // Feature flags are configured under the "FeatureManagement" section in appsettings.
        // Example: { "FeatureManagement": { "EnableApiDocs": true } }
        services.AddFeatureManagement();

        return services;
    }

    public static void MapApiEndpoints(this WebApplication app)
    {
        // A-31: Feature flag via IFeatureManager instead of raw config boolean.
        // The "EnableApiDocs" flag maps to { "FeatureManagement": { "EnableApiDocs": true } }
        // in appsettings. Raw config ("Features:EnableApiDocs") still works as a fallback because
        // FeatureManagement reads from both sections.
        var featureManager = app.Services.GetRequiredService<IFeatureManager>();
        var apiDocsEnabled = featureManager.IsEnabledAsync("EnableApiDocs").GetAwaiter().GetResult();

        if (app.Environment.IsDevelopment() || (app.Environment.IsStaging() && apiDocsEnabled))
        {
            app.MapOpenApi();
            app.MapScalarApiReference(opt =>
            {
                opt
                    .WithTitle("BudgetBuddy API")
                    .WithTheme(ScalarTheme.Purple)
                    .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
            });
        }

        app.MapCarter();
    }
}

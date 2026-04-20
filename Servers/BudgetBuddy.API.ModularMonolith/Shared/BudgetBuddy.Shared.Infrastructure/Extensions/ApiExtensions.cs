using Asp.Versioning;
using BudgetBuddy.Shared.Infrastructure.Exceptions;
using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Services;
using BudgetBuddy.Shared.Infrastructure.Behaviors;
using Carter;
using FluentValidation;
using Mapster;
using NodaTime.Serialization.SystemTextJson;
using Scalar.AspNetCore;

namespace BudgetBuddy.Shared.Infrastructure.Extensions;

public static class ApiExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration,
        params System.Reflection.Assembly[] moduleAssemblies)
    {
        // NOTE: ValidateConfigurations is NOT called here.
        // It must be called after WebApplication.Build() so that WebApplicationFactory
        // test overrides (via ConfigureAppConfiguration) are already in the configuration.
        // See Program.cs for the call site.

        // HttpContextAccessor for accessing HttpContext in handlers
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, HttpContextCurrentUserService>();

        // NodaTime + Enum as String
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
            options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        });
        services.AddSingleton<IClock>(SystemClock.Instance);

        // MediatR - scan shared infrastructure + all module assemblies
        var allAssemblies = new[]
        {
            typeof(ApiExtensions).Assembly,
        }.Concat(moduleAssemblies).ToArray();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(allAssemblies));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CacheInvalidationBehavior<,>));

        // FluentValidation - scan all assemblies
        foreach (var assembly in allAssemblies)
            services.AddValidatorsFromAssembly(assembly);

        // Exception handling
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // API Versioning - implicit v1.0 for now, explicit versioning deferred until v2 needed
        services.AddApiVersioning(opt =>
        {
            opt.DefaultApiVersion = new ApiVersion(1, 0);
            opt.AssumeDefaultVersionWhenUnspecified = true;
            opt.ReportApiVersions = true; // Response header: api-supported-versions
        });

        // Carter
        services.AddCarter(configurator: c =>
        {
            foreach (var assembly in allAssemblies)
                c.WithModules(assembly.GetTypes()
                    .Where(t => t.IsAssignableTo(typeof(ICarterModule)) && !t.IsAbstract)
                    .ToArray());
        });

        // Mapster - Object mapping
        var mapsterConfig = TypeAdapterConfig.GlobalSettings;
        foreach (var assembly in allAssemblies)
            mapsterConfig.Scan(assembly);
        services.AddSingleton(mapsterConfig);
        services.AddMapster();

        // OpenAPI
        services.AddOpenApi();

        return services;
    }

    public static void ValidateConfigurations(IConfiguration configuration)
    {
        var requiredSettings = new[]
        {
            "ConnectionStrings:DefaultConnection",
            "RequestTimeThresholdSeconds",
            "ShutdownTimeoutSeconds",
            "RequestTimeoutSeconds"
        };

        foreach (var setting in requiredSettings)
        {
            if (string.IsNullOrEmpty(configuration[setting]))
                throw new InvalidOperationException($"Required configuration '{setting}' is missing.");
        }

        if (configuration.GetSection("RateLimit").Exists() == false)
            throw new InvalidOperationException("Required configuration section 'RateLimit' is missing.");
    }

    public static void MapApiEndpoints(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
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
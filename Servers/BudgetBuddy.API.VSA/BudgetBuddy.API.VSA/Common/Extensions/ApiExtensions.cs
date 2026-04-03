using Asp.Versioning;
using BudgetBuddy.API.VSA.Common.Exceptions;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.Services;
using BudgetBuddy.API.VSA.Common.Shared.Behaviors;
using FluentValidation;
using Mapster;
using NodaTime.Serialization.SystemTextJson;
using Scalar.AspNetCore;

namespace BudgetBuddy.API.VSA.Common.Extensions;

public static class ApiExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        // NOTE: ValidateConfigurations is NOT called here.
        // It must be called after WebApplication.Build() so that WebApplicationFactory
        // test overrides (via ConfigureAppConfiguration) are already in the configuration.
        // See Program.cs for the call site.

        // HttpContextAccessor for accessing HttpContext in handlers
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, HttpContextCurrentUserService>();
        services.AddScoped<IUserCurrencyService, UserCurrencyService>();

        // NodaTime + Enum as String
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
            options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        });
        services.AddSingleton<IClock>(SystemClock.Instance);

        // MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CacheInvalidationBehavior<,>));

        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<Program>();
        //services.AddFluentValidationAutoValidation();
        //services.AddFluentValidationClientsideAdapters();

        // Exception handling 
        services.AddProblemDetails();
        //services.AddExceptionHandler<ValidationExceptionHandler>();
        //services.AddExceptionHandler<DomainExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // API Versioning - implicit v1.0 for now, explicit versioning deferred until v2 needed
        services.AddApiVersioning(opt =>
        {
            opt.DefaultApiVersion = new ApiVersion(1, 0);
            opt.AssumeDefaultVersionWhenUnspecified = true;
            opt.ReportApiVersions = true; // Response header: api-supported-versions
        });

        // Carter
        services.AddCarter();

        // Mapster - Object mapping
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(typeof(Program).Assembly);
        services.AddSingleton(config);
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
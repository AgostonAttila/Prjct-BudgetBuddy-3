using BudgetBuddy.Application.Features.BudgetAlerts.Services;
using BudgetBuddy.Application.Features.Dashboard.Services;
using BudgetBuddy.Application.Features.Investments.Services;
using BudgetBuddy.Application.Features.MarketData.Services;
using BudgetBuddy.Application.Features.Reports.Services;
using BudgetBuddy.Application.Features.Transactions.Services;
using BudgetBuddy.Application.Features.Transfers.Services;
using BudgetBuddy.Application.Features.TwoFactor.Services;
using BudgetBuddy.Application.Features.UserSettings.Services;
using BudgetBuddy.Infrastructure.BackgroundJobs;
using BudgetBuddy.Infrastructure.DataExchange;
using BudgetBuddy.Infrastructure.Financial;
using BudgetBuddy.Infrastructure.Financial.Providers;
using BudgetBuddy.Infrastructure.Notification.Email;
using BudgetBuddy.Infrastructure.Security.Authentication;
using BudgetBuddy.Infrastructure.Security.Encryption;
using BudgetBuddy.Infrastructure.Security.Filescanning;
using BudgetBuddy.Infrastructure.Services;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using System.Net;

namespace BudgetBuddy.Infrastructure.Extensions;

public static class InfrastructureServicesExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddInfrastructureServices(IConfiguration configuration)
        {
            // Infrastructure settings bindings
            services.AddOptions<EmailSettings>()
                .Bind(configuration.GetSection(EmailSettings.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddOptions<BackgroundJobsSettings>()
                .Bind(configuration.GetSection(BackgroundJobsSettings.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddOptions<ExchangeRateSettings>()
                .Bind(configuration.GetSection(ExchangeRateSettings.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddOptions<PriceServiceSettings>()
                .Bind(configuration.GetSection(PriceServiceSettings.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // HTTP Context (required by HttpContextCurrentUserService)
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, HttpContextCurrentUserService>();
            services.AddScoped<IUserCurrencyService, UserCurrencyService>();
            services.AddSingleton<IClock>(SystemClock.Instance);

            // Security Services
            services.AddSingleton<IDataProtectionService, DataProtectionService>();
            services.AddSingleton<IEncryptionService, EncryptionService>();
            services.AddSingleton<IAntivirusService, ClamAVService>();
            services.AddSingleton<ITokenBlacklistService, TokenBlacklistService>();
            services.AddScoped<ISecurityEventService, SecurityEventService>();
            services.AddScoped<ITokenService, TokenService>();

            // Financial services
            services.AddPriceProviders();
            services.AddCurrencyConversion();

            // Market data backfill
            services.AddScoped<IMarketDataBackfillService, MarketDataBackfillService>();

            // Notification Services
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IAuthenticationEmailService, AuthenticationEmailService>();

            // Export Services
            services.AddScoped<ICsvExportService, CsvExportService>();
            services.AddScoped<IExcelExportService, ExcelExportService>();
            services.AddScoped<IDataImportService, ExcelImportService>();
            services.AddScoped<IExportFactory, ExportFactory>();

            // Shared Services
            services.AddScoped<IUserCacheInvalidator, UserCacheInvalidator>();

            // Calculation Services
            services.AddScoped<IInvestmentCalculationService, InvestmentCalculationService>();
            services.AddScoped<CurrencyConverter>();
            services.AddScoped<IAccountBalanceService, AccountBalanceService>();

            // Domain Services
            services.AddScoped<BudgetAlertService>();
            services.AddScoped<IBudgetAlertService>(sp => sp.GetRequiredService<BudgetAlertService>());
            services.AddScoped<IBudgetAlertCalculationService>(sp => sp.GetRequiredService<BudgetAlertService>());
            services.AddScoped<IBudgetAlertRuleEngine>(sp => sp.GetRequiredService<BudgetAlertService>());
            services.AddScoped<IBudgetAlertMessageFormatter>(sp => sp.GetRequiredService<BudgetAlertService>());
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IPortfolioService, PortfolioService>();
            services.AddScoped<ReportService>();
            services.AddScoped<IReportService>(sp => sp.GetRequiredService<ReportService>());
            services.AddScoped<IIncomeExpenseReportService>(sp => sp.GetRequiredService<ReportService>());
            services.AddScoped<IMonthlySummaryReportService>(sp => sp.GetRequiredService<ReportService>());
            services.AddScoped<ISpendingReportService>(sp => sp.GetRequiredService<ReportService>());
            services.AddScoped<IInvestmentReportService>(sp => sp.GetRequiredService<ReportService>());
            services.AddScoped<ITransferService, TransferService>();
            services.AddScoped<ITransactionValidationService, TransactionValidationService>();
            services.AddScoped<ITwoFactorService, TwoFactorService>();
            services.AddScoped<IUserSettingsService, UserSettingsService>();
            services.AddSingleton<IAppCache, HybridAppCache>();

            return services;
        }

        private void AddPriceProviders()
        {
            services.AddHttpClient<CoinGeckoPriceProvider>((sp, client) =>
                {
                    var s = sp.GetRequiredService<IOptions<PriceServiceSettings>>().Value;
                    client.BaseAddress = new Uri(s.CoinGeckoBaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(s.TimeoutSeconds);
                    if (!string.IsNullOrEmpty(s.CoinGeckoApiKey))
                        client.DefaultRequestHeaders.Add("x-cg-demo-api-key", s.CoinGeckoApiKey);
                })
                .AddResilienceHandler("coingecko", builder =>
                {
                    builder.AddRetry(new HttpRetryStrategyOptions
                    {
                        MaxRetryAttempts = 2,
                        Delay = TimeSpan.FromSeconds(1),
                        BackoffType = DelayBackoffType.Exponential,
                        ShouldHandle = args => args.Outcome switch
                        {
                            { Exception: HttpRequestException } => PredicateResult.True(),
                            { Result.StatusCode: HttpStatusCode.TooManyRequests } => PredicateResult.False(),
                            { Result: { IsSuccessStatusCode: false } } => PredicateResult.True(),
                            _ => PredicateResult.False()
                        }
                    });
                    builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                    {
                        SamplingDuration = TimeSpan.FromSeconds(30),
                        FailureRatio = 0.8,
                        MinimumThroughput = 3,
                        BreakDuration = TimeSpan.FromSeconds(15)
                    });
                });

            services.AddHttpClient<YahooFinancePriceProvider>((sp, client) =>
                {
                    var s = sp.GetRequiredService<IOptions<PriceServiceSettings>>().Value;
                    client.BaseAddress = new Uri(s.YahooFinanceBaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(s.TimeoutSeconds);
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; BudgetBuddy/1.0)");
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
                })
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    CookieContainer = new System.Net.CookieContainer(),
                    UseCookies = true,
                    AllowAutoRedirect = true,
                })
                .AddStandardResilienceHandler(options =>
                {
                    options.Retry.MaxRetryAttempts = 3;
                    options.Retry.Delay = TimeSpan.FromSeconds(1);
                    options.Retry.BackoffType = DelayBackoffType.Exponential;
                    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(20);
                });

            // Interface registration must go through GetRequiredService<ConcreteType>() so that
            // the typed client factory (AddHttpClient<T>) provides the configured HttpClient.
            services.AddScoped<IPriceProvider>(sp => sp.GetRequiredService<CoinGeckoPriceProvider>());
            services.AddScoped<IPriceProvider>(sp => sp.GetRequiredService<YahooFinancePriceProvider>());
            services.AddScoped<IPriceService, MarketDataPriceService>();
        }

        private void AddCurrencyConversion()
        {
            services.AddHttpClient<FrankfurterCurrencyConversionService>((sp, client) =>
                {
                    var s = sp.GetRequiredService<IOptions<ExchangeRateSettings>>().Value;
                    client.BaseAddress = new Uri(s.BaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(s.TimeoutSeconds);
                })
                .AddStandardResilienceHandler(options =>
                {
                    options.Retry.MaxRetryAttempts = 2;
                    options.Retry.Delay = TimeSpan.FromSeconds(2);
                    options.Retry.BackoffType = DelayBackoffType.Exponential;
                });

            services.AddScoped<ICurrencyConversionService>(
                sp => sp.GetRequiredService<FrankfurterCurrencyConversionService>());

            services.AddScoped<IFxHistoricalProvider>(
                sp => sp.GetRequiredService<FrankfurterCurrencyConversionService>());

            services.AddScoped<IHistoricalPriceProvider>(sp => sp.GetRequiredService<CoinGeckoPriceProvider>());
            services.AddScoped<IHistoricalPriceProvider>(sp => sp.GetRequiredService<YahooFinancePriceProvider>());
            services.AddScoped<IHistoricalPriceService, MarketDataHistoricalPriceService>();
        }
    }
}

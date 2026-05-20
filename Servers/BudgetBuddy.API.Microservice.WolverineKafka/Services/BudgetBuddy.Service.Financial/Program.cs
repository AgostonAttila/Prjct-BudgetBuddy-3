using BudgetBuddy.Service.Financial.Financial;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability("financial-service");
builder.AddSecurity(builder.Configuration);
builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddApiServices(builder.Configuration, typeof(Program).Assembly);
builder.Services.AddCaching(builder.Configuration, builder.Environment);
builder.Services.AddCompression();

// Frankfurter FX — required by YahooFinancePriceProvider for currency conversion
builder.Services.AddCurrencyConversionService(builder.Configuration);

// CoinGecko + Yahoo Finance HttpClients + MarketDataPriceService (IPriceService)
builder.Services.AddFinancialHttpClients(builder.Configuration);

var app = builder.Build();

app.UseMiddlewarePipeline();
app.UseSecurity();
app.UseAuthentication();
app.UseAuthorization();
app.MapObservabilityEndpoints();
app.MapApiEndpoints();

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "financial-service terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

using BudgetBuddy.API.VSA.Common.Extensions;
using BudgetBuddy.API.VSA.Common.Infrastructure.BackgroundJobs;
using Quartz;

namespace BudgetBuddy.API.VSA.Features.MarketData.Jobs;

public static class MarketDataJobExtensions
{
    private const string PriceSection = "BackgroundJobs:DailyPriceSnapshot";
    private const string FxSection = "BackgroundJobs:DailyFxSnapshot";
    private const string BackfillSection = "BackgroundJobs:BackfillMarketData";

    public static IServiceCollectionQuartzConfigurator AddMarketDataJobs(
        this IServiceCollectionQuartzConfigurator q, IConfiguration configuration)
    {
        var priceSettings = configuration.GetSection(PriceSection).Get<JobSettings>() ?? new();
        if (priceSettings.Enabled)
            q.AddJobWithCronTrigger<DailyPriceSnapshotJob>(priceSettings.CronExpression);

        var fxSettings = configuration.GetSection(FxSection).Get<JobSettings>() ?? new();
        if (fxSettings.Enabled)
            q.AddJobWithCronTrigger<DailyFxSnapshotJob>(fxSettings.CronExpression);

        var backfillSettings = configuration.GetSection(BackfillSection).Get<JobSettings>() ?? new();
        if (backfillSettings.Enabled)
            q.AddJobWithCronTrigger<BackfillMarketDataJob>(backfillSettings.CronExpression);

        return q;
    }
}

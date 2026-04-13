using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;

namespace BudgetBuddy.API.Extensions;

public static class CompressionExtensions
{
    public static void AddCompression(this IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });

        services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            // Optimal provides best compression ratio with acceptable performance
            // ~10% better compression than Fastest, ~50ms slower on 100KB payload
            options.Level = CompressionLevel.Optimal;
        });

        services.Configure<GzipCompressionProviderOptions>(options =>
        {
            // Optimal balances compression ratio and speed
            // SmallestSize adds ~200ms latency for minimal compression gain
            options.Level = CompressionLevel.Optimal;
        });
    }
}
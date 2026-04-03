using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace BudgetBuddy.API.VSA.Features.TwoFactor.GetRecoveryCodes;

public class GetRecoveryCodesHandler(
    UserManager<User> userManager,
    IDistributedCache cache,
    ILogger<GetRecoveryCodesHandler> logger) : IRequestHandler<GetRecoveryCodesQuery, RecoveryCodesResponse>
{
    private const int CodesPerBatch = 5;
    private const int TotalCodes = 10;
    private const int TotalBatches = 2;
    private const int CacheExpirationMinutes = 15;

    public async Task<RecoveryCodesResponse> Handle(
        GetRecoveryCodesQuery request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(request.User);
        if (user == null)
        {
            logger.LogWarning("User not found when attempting to generate recovery codes");
            throw new UnauthorizedAccessException("User not found");
        }

        // Check if 2FA is enabled
        var isTwoFactorEnabled = await userManager.GetTwoFactorEnabledAsync(user);
        if (!isTwoFactorEnabled)
        {
            logger.LogWarning("Attempted to generate recovery codes for user {UserId} without 2FA enabled", user.Id);
            throw new InvalidOperationException("Two-factor authentication must be enabled to generate recovery codes");
        }

        // Validate batch number
        if (request.BatchNumber < 1 || request.BatchNumber > TotalBatches)
        {
            throw new ArgumentException($"Batch number must be between 1 and {TotalBatches}");
        }

        var cacheKey = $"recovery-codes:{user.Id}";
        string[] allCodes;

        if (request.GenerateNew)
        {
            // Generate new recovery codes
            logger.LogWarning("Generating NEW recovery codes for user {UserId} - ALL OLD CODES INVALIDATED", user.Id);

            var recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, TotalCodes);
            allCodes = recoveryCodes!.ToArray();

            // Store in cache temporarily for batch retrieval (15 minutes)
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes)
            };

            var serializedCodes = JsonSerializer.Serialize(allCodes);
            await cache.SetStringAsync(cacheKey, serializedCodes, cacheOptions, cancellationToken);

            logger.LogInformation("Recovery codes generated and cached for user {UserId}. Expires in {Minutes} minutes",
                user.Id, CacheExpirationMinutes);
        }
        else
        {
            // Retrieve existing codes from cache
            var cachedCodes = await cache.GetStringAsync(cacheKey, cancellationToken);

            if (string.IsNullOrEmpty(cachedCodes))
            {
                logger.LogWarning("User {UserId} attempted to retrieve batch {BatchNumber} but no codes in cache",
                    user.Id, request.BatchNumber);
                throw new InvalidOperationException(
                    "No recovery codes found. Please generate new codes with GenerateNew=true");
            }

            allCodes = JsonSerializer.Deserialize<string[]>(cachedCodes)!;

            logger.LogInformation("User {UserId} retrieved batch {BatchNumber} of recovery codes from cache",
                user.Id, request.BatchNumber);
        }

        // Return requested batch
        var startIndex = (request.BatchNumber - 1) * CodesPerBatch;
        var batchCodes = allCodes.Skip(startIndex).Take(CodesPerBatch).ToArray();

        // If this is the last batch, remove from cache
        var hasMoreBatches = request.BatchNumber < TotalBatches;
        if (!hasMoreBatches)
        {
            await cache.RemoveAsync(cacheKey, cancellationToken);
            logger.LogInformation("All batches retrieved for user {UserId}. Codes removed from cache", user.Id);
        }

        return new RecoveryCodesResponse(
            RecoveryCodes: batchCodes,
            TotalBatches: TotalBatches,
            CurrentBatch: request.BatchNumber,
            HasMoreBatches: hasMoreBatches,
            SecurityWarning: "CRITICAL: Store these codes securely. Each code can only be used once. " +
                           "If compromised, generate new codes immediately. Codes are hashed in database."
        );
    }
}

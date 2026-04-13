using BudgetBuddy.Application.Common.Contracts;
using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Features.TwoFactor.Services;

namespace BudgetBuddy.Application.Features.TwoFactor.GetRecoveryCodes;

public class GetRecoveryCodesHandler(
    ITwoFactorService twoFactorService,
    IAppCache appCache,
    ICurrentUserService currentUserService,
    ILogger<GetRecoveryCodesHandler> logger)
    : UserAwareHandler<GetRecoveryCodesQuery, RecoveryCodesResponse>(currentUserService)
{
    private const int CodesPerBatch = 5;
    private const int TotalCodes = 10;
    private const int TotalBatches = 2;
    private const int CacheExpirationMinutes = 15;

    public override async Task<RecoveryCodesResponse> Handle(
        GetRecoveryCodesQuery request,
        CancellationToken cancellationToken)
    {
        var status = await twoFactorService.GetStatusAsync(UserId, cancellationToken);
        if (!status.IsEnabled)
        {
            logger.LogWarning("Attempted to generate recovery codes for user {UserId} without 2FA enabled", UserId);
            throw new InvalidOperationException("Two-factor authentication must be enabled to generate recovery codes");
        }

        if (request.BatchNumber < 1 || request.BatchNumber > TotalBatches)
            throw new ArgumentException($"Batch number must be between 1 and {TotalBatches}");

        var cacheKey = $"recovery-codes:{UserId}";
        var cacheOptions = new AppCacheOptions(Ttl: TimeSpan.FromMinutes(CacheExpirationMinutes));
        string[] allCodes;

        if (request.GenerateNew)
        {
            logger.LogWarning("Generating NEW recovery codes for user {UserId} — ALL OLD CODES INVALIDATED", UserId);

            var generated = await twoFactorService.GenerateRecoveryCodesAsync(UserId, TotalCodes, cancellationToken);
            allCodes = generated.ToArray();

            // Remove any existing cached codes first, then cache the freshly generated ones.
            await appCache.RemoveAsync(cacheKey, cancellationToken);
            await appCache.GetOrCreateAsync(
                cacheKey,
                _ => ValueTask.FromResult(allCodes),
                cacheOptions,
                ct: cancellationToken);

            logger.LogInformation(
                "Recovery codes generated and cached for user {UserId}. Expires in {Minutes} minutes",
                UserId, CacheExpirationMinutes);
        }
        else
        {
            // Factory is only called on a cache miss — meaning the 15-minute window expired
            // or codes were never generated. Throw instead of silently returning empty codes.
            allCodes = await appCache.GetOrCreateAsync<string[]>(
                cacheKey,
                _ =>
                {
                    logger.LogWarning(
                        "User {UserId} attempted to retrieve batch {BatchNumber} but no codes in cache",
                        UserId, request.BatchNumber);
                    throw new InvalidOperationException(
                        "No recovery codes found. Please generate new codes with GenerateNew=true");
                },
                cacheOptions,
                ct: cancellationToken);

            logger.LogInformation("User {UserId} retrieved batch {BatchNumber} of recovery codes from cache",
                UserId, request.BatchNumber);
        }

        var startIndex = (request.BatchNumber - 1) * CodesPerBatch;
        var batchCodes = allCodes.Skip(startIndex).Take(CodesPerBatch).ToArray();

        var hasMoreBatches = request.BatchNumber < TotalBatches;
        if (!hasMoreBatches)
        {
            await appCache.RemoveAsync(cacheKey, cancellationToken);
            logger.LogInformation("All batches retrieved for user {UserId}. Codes removed from cache", UserId);
        }

        return new RecoveryCodesResponse(
            RecoveryCodes: batchCodes,
            TotalBatches: TotalBatches,
            CurrentBatch: request.BatchNumber,
            HasMoreBatches: hasMoreBatches,
            SecurityWarning: "CRITICAL: Store these codes securely. Each code can only be used once. " +
                             "If compromised, generate new codes immediately. Codes are hashed in database.");
    }
}

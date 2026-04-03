using Microsoft.AspNetCore.Mvc;

namespace BudgetBuddy.API.VSA.Features.TwoFactor.GetRecoveryCodes;

public class GetRecoveryCodesEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/twofactor/recovery-codes", async (
            HttpContext context,
            IMediator mediator,
            [FromQuery] int batchNumber = 1,
            [FromQuery] bool generateNew = false,
            CancellationToken cancellationToken = default) =>
        {
            var query = new GetRecoveryCodesQuery(
                User: context.User,
                BatchNumber: batchNumber,
                GenerateNew: generateNew
            );

            var result = await mediator.Send(query, cancellationToken);
            return Results.Ok(result);
        })
        .WithSummary("Generate or retrieve recovery codes in batches")
        .WithDescription(@"
SECURITY: Generates or retrieves backup recovery codes for account recovery.

**Usage:**
1. First request: POST /api/twofactor/recovery-codes?generateNew=true&batchNumber=1
   - Generates 10 new codes, returns first 5 (batch 1)
   - All old codes are INVALIDATED

2. Second request: POST /api/twofactor/recovery-codes?batchNumber=2
   - Retrieves remaining 5 codes (batch 2)
   - Codes expire from cache after 15 minutes

**Security Features:**
- Codes are hashed before database storage (like passwords)
- Batched distribution reduces exposure if request is intercepted
- Temporary cache storage with automatic expiration
- Auth-level rate limiting to prevent brute force
- Each code can only be used once

**Requirements:**
- User must be authenticated
- 2FA must be enabled on account")
        .WithAuthRateLimit()  // Stricter rate limiting for security-sensitive operation
        .RequireAuthorization()
        .WithTags("TwoFactor")
        .WithName("GetRecoveryCodes")
        ;
    }
}

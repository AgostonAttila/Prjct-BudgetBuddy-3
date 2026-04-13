using System.Security.Claims;

namespace BudgetBuddy.Application.Features.TwoFactor.GetRecoveryCodes;

public record GetRecoveryCodesQuery(
    ClaimsPrincipal User,
    int BatchNumber = 1,  // 1 or 2 (batch 1: codes 1-5, batch 2: codes 6-10)
    bool GenerateNew = false  // true to generate new codes, false to retrieve existing batch
) : IRequest<RecoveryCodesResponse>;

public record RecoveryCodesResponse(
    string[] RecoveryCodes,
    int TotalBatches,
    int CurrentBatch,
    bool HasMoreBatches,
    string SecurityWarning
);

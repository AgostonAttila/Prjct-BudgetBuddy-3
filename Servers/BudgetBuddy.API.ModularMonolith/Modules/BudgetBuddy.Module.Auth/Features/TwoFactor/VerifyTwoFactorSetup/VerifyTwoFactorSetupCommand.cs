using FluentValidation;
using MediatR;
using System.Security.Claims;

namespace BudgetBuddy.Module.Auth.Features.TwoFactor.VerifyTwoFactorSetup;

public record VerifyTwoFactorSetupCommand(
    ClaimsPrincipal User,
    string Code
) : IRequest<VerifyTwoFactorSetupResponse>;

public record VerifyTwoFactorSetupResponse(
    bool Success,
    string Message
);

public class VerifyTwoFactorSetupValidator : AbstractValidator<VerifyTwoFactorSetupCommand>
{
    public VerifyTwoFactorSetupValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Verification code is required")
            .Length(6).WithMessage("Verification code must be 6 digits")
            .Matches(@"^\d{6}$").WithMessage("Verification code must contain only digits");
    }
}

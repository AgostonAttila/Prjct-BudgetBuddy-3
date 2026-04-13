using FluentValidation;
using System.Security.Claims;

namespace BudgetBuddy.Application.Features.TwoFactor.DisableTwoFactor;

public record DisableTwoFactorCommand(
    ClaimsPrincipal User,
    string Password
) : IRequest<DisableTwoFactorResponse>;

public record DisableTwoFactorResponse(
    bool Success,
    string Message
);

public class DisableTwoFactorValidator : AbstractValidator<DisableTwoFactorCommand>
{
    public DisableTwoFactorValidator()
    {
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required for disabling 2FA");
    }
}

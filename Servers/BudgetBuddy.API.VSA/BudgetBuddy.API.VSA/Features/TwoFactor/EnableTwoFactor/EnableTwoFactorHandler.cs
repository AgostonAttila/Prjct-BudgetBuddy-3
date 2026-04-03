using Microsoft.AspNetCore.Identity;
using QRCoder;

namespace BudgetBuddy.API.VSA.Features.TwoFactor.EnableTwoFactor;

public class EnableTwoFactorHandler(
    UserManager<User> userManager,
    ILogger<EnableTwoFactorHandler> logger) : IRequestHandler<EnableTwoFactorCommand, EnableTwoFactorResponse>
{
    public async Task<EnableTwoFactorResponse> Handle(
        EnableTwoFactorCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(request.User);
        if (user == null)
        {
            logger.LogWarning("User not found when attempting to enable 2FA");
            throw new UnauthorizedAccessException("User not found");
        }

        logger.LogInformation("Generating 2FA setup for user {UserId}", user.Id);

        // Generate or retrieve authenticator key
        var key = await userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            await userManager.ResetAuthenticatorKeyAsync(user);
            key = await userManager.GetAuthenticatorKeyAsync(user);
        }

        // Generate QR code URI (Google Authenticator compatible)
        var email = await userManager.GetEmailAsync(user);
        var authenticatorUri = GenerateQrCodeUri(email!, key!);

        // Generate QR code as base64 image
        var qrCodeDataUrl = GenerateQrCodeImage(authenticatorUri);

        logger.LogInformation("2FA setup generated successfully for user {UserId}", user.Id);

        return new EnableTwoFactorResponse(
            SharedKey: FormatKey(key!),
            QrCodeDataUrl: qrCodeDataUrl
        );
    }

    private static string GenerateQrCodeUri(string email, string key)
    {
        const string issuer = "BudgetBuddy";
        return $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}?secret={key}&issuer={Uri.EscapeDataString(issuer)}&digits=6";
    }

    private static string GenerateQrCodeImage(string authenticatorUri)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(authenticatorUri, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeImage = qrCode.GetGraphic(20);
        return $"data:image/png;base64,{Convert.ToBase64String(qrCodeImage)}";
    }

    private static string FormatKey(string key)
    {
        // Format: "ABCDEFGHIJKLMNOP" -> "ABCD EFGH IJKL MNOP"
        var chunkSize = 4;
        var chunks = new List<string>();
        for (int i = 0; i < key.Length; i += chunkSize)
        {
            var length = Math.Min(chunkSize, key.Length - i);
            chunks.Add(key.Substring(i, length));
        }
        return string.Join(" ", chunks);
    }
}

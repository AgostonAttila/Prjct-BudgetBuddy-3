using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace BudgetBuddy.API.VSA.Common.Infrastructure.Security.Authentication;

public class TokenService(IConfiguration configuration, ILogger<TokenService> logger) : ITokenService
{
    private readonly ILogger<TokenService> _logger = logger;

    public string GenerateAccessToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured"));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add roles (if you have role system)
        // var roles = await _userManager.GetRolesAsync(user);
        // claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(15), // 15 minutes (short-lived)
            Issuer = configuration["Jwt:Issuer"],
            Audience = configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshTokenString()
    {
        // Generate cryptographically secure random token (512 bits = 64 bytes)
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        return Convert.ToBase64String(randomBytes);
    }

    public RefreshToken CreateRefreshToken(User user, string? ipAddress)
    {
        // Generate plain token (512-bit cryptographically secure random)
        var plainToken = GenerateRefreshTokenString();

        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = plainToken,  // Will be hashed by EF HashedStringConverter on save
            PlainToken = plainToken,  // Store plain token for response (not persisted)
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7), // 7 days
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };
    }
}

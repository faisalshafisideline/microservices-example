using AuthService.Domain.Entities;
using System.Security.Claims;

namespace AuthService.Application.Services;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user, List<Claim>? additionalClaims = null);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
    DateTime GetTokenExpiration(string token);
    bool IsTokenExpired(string token);
}

public record TokenPair(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt
); 
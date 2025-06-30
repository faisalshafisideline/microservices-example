using AuthService.Application.Services;
using AuthService.Domain.Entities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AuthService.Infrastructure.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly SymmetricSecurityKey _signingKey;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
        _tokenHandler = new JwtSecurityTokenHandler();
        
        var secretKey = _configuration["Jwt:SecretKey"] ?? 
            throw new InvalidOperationException("JWT SecretKey is not configured");
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    }

    public string GenerateAccessToken(User user, List<Claim>? additionalClaims = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.Value.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new("preferred_language", user.PreferredLanguage),
            new("email_verified", user.EmailVerified.ToString().ToLower()),
            new("two_factor_enabled", user.TwoFactorEnabled.ToString().ToLower())
        };

        // Add club roles as claims for multi-tenant authorization
        foreach (var clubRole in user.ClubRoles)
        {
            claims.Add(new Claim("club_id", clubRole.ClubId.ToString()));
            
            // Split comma-separated roles and add each as a separate claim
            var roles = clubRole.Role.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var role in roles)
            {
                claims.Add(new Claim($"club_{clubRole.ClubId}_role", role.Trim()));
                claims.Add(new Claim(ClaimTypes.Role, $"{clubRole.ClubId}:{role.Trim()}"));
            }
        }

        // Add any additional claims
        if (additionalClaims != null)
        {
            claims.AddRange(additionalClaims);
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(GetAccessTokenExpirationMinutes()),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256)
        };

        var token = _tokenHandler.CreateToken(tokenDescriptor);
        return _tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidAudience = _configuration["Jwt:Audience"],
                IssuerSigningKey = _signingKey,
                ClockSkew = TimeSpan.Zero
            };

            var principal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    public DateTime GetTokenExpiration(string token)
    {
        try
        {
            var jsonToken = _tokenHandler.ReadJwtToken(token);
            return jsonToken.ValidTo;
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    public bool IsTokenExpired(string token)
    {
        var expiration = GetTokenExpiration(token);
        return expiration <= DateTime.UtcNow;
    }

    private int GetAccessTokenExpirationMinutes()
    {
        return _configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes", 15);
    }
} 
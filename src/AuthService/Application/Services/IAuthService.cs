using AuthService.Application.DTOs;

namespace AuthService.Application.Services;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string usernameOrEmail, string password, string? twoFactorCode = null);
    Task<AuthResult> RefreshTokenAsync(string refreshToken);
    Task<bool> RevokeTokenAsync(string refreshToken);
    Task<RegisterResult> RegisterAsync(RegisterRequest request);
    Task<bool> VerifyEmailAsync(string email, string verificationCode);
    Task<bool> ResetPasswordAsync(string email, string resetCode, string newPassword);
    Task<TwoFactorSetupResult> SetupTwoFactorAsync(Guid userId);
    Task<bool> EnableTwoFactorAsync(Guid userId, string verificationCode);
    Task<bool> DisableTwoFactorAsync(Guid userId, string password);
    Task<UserProfileDto?> GetUserProfileAsync(Guid userId);
    Task<bool> UpdateUserProfileAsync(Guid userId, UpdateProfileRequest request);
}

public record AuthResult(
    bool Success,
    string? AccessToken,
    string? RefreshToken,
    DateTime? ExpiresAt,
    UserProfileDto? User,
    string? ErrorMessage,
    bool RequiresTwoFactor = false
);

public record RegisterResult(
    bool Success,
    Guid? UserId,
    string? ErrorMessage,
    bool RequiresEmailVerification = true
);

public record TwoFactorSetupResult(
    bool Success,
    string? Secret,
    string? QrCodeUrl,
    string[]? BackupCodes,
    string? ErrorMessage
); 
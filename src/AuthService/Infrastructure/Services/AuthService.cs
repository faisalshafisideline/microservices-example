using AuthService.Application.DTOs;
using AuthService.Application.Services;
using AuthService.Domain.Entities;
using AuthService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using OtpNet;

namespace AuthService.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AuthDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AuthDbContext context,
        IJwtTokenService jwtTokenService,
        ILogger<AuthService> logger)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<AuthResult> LoginAsync(string usernameOrEmail, string password, string? twoFactorCode = null)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.ClubRoles)
                .FirstOrDefaultAsync(u => 
                    u.Email == usernameOrEmail.ToLowerInvariant() || 
                    u.Username == usernameOrEmail.ToLowerInvariant());

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                _logger.LogWarning("Failed login attempt for {UsernameOrEmail}", usernameOrEmail);
                return new AuthResult(false, null, null, null, null, "Invalid credentials");
            }

            if (!user.IsActive)
            {
                return new AuthResult(false, null, null, null, null, "Account is deactivated");
            }

            // Check 2FA if enabled
            if (user.TwoFactorEnabled)
            {
                if (string.IsNullOrEmpty(twoFactorCode))
                {
                    return new AuthResult(false, null, null, null, null, "Two-factor authentication required", true);
                }

                if (!ValidateTwoFactorCode(user.TwoFactorSecret!, twoFactorCode))
                {
                    _logger.LogWarning("Invalid 2FA code for user {UserId}", user.Id);
                    return new AuthResult(false, null, null, null, null, "Invalid two-factor code");
                }
            }

            // Generate tokens
            var accessToken = _jwtTokenService.GenerateAccessToken(user);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();
            var expiresAt = _jwtTokenService.GetTokenExpiration(accessToken);

            // Update last login
            user.RecordLogin();
            await _context.SaveChangesAsync();

            var userProfile = MapToUserProfileDto(user);

            _logger.LogInformation("Successful login for user {UserId}", user.Id);
            
            return new AuthResult(true, accessToken, refreshToken, expiresAt, userProfile, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {UsernameOrEmail}", usernameOrEmail);
            return new AuthResult(false, null, null, null, null, "An error occurred during login");
        }
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken)
    {
        // TODO: Implement refresh token validation and storage
        // For now, return error
        return new AuthResult(false, null, null, null, null, "Refresh token functionality not implemented yet");
    }

    public async Task<bool> RevokeTokenAsync(string refreshToken)
    {
        // TODO: Implement refresh token revocation
        return true;
    }

    public async Task<RegisterResult> RegisterAsync(RegisterRequest request)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant() || 
                                         u.Username == request.Username.ToLowerInvariant());

            if (existingUser != null)
            {
                return new RegisterResult(false, null, "User with this email or username already exists");
            }

            // Create new user
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var user = new User(
                request.Email,
                request.Username,
                passwordHash,
                request.FirstName,
                request.LastName,
                request.PreferredLanguage
            );

            // Add initial club role if specified
            if (request.InitialClubId.HasValue)
            {
                user.AddClubRole(request.InitialClubId.Value, request.InitialRole);
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("New user registered: {UserId} - {Email}", user.Id, user.Email);

            return new RegisterResult(true, user.Id.Value, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration for {Email}", request.Email);
            return new RegisterResult(false, null, "An error occurred during registration");
        }
    }

    public async Task<bool> VerifyEmailAsync(string email, string verificationCode)
    {
        // TODO: Implement email verification with codes
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());

        if (user != null)
        {
            user.VerifyEmail();
            await _context.SaveChangesAsync();
            return true;
        }

        return false;
    }

    public async Task<bool> ResetPasswordAsync(string email, string resetCode, string newPassword)
    {
        // TODO: Implement password reset with codes
        return false;
    }

    public async Task<TwoFactorSetupResult> SetupTwoFactorAsync(Guid userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return new TwoFactorSetupResult(false, null, null, null, "User not found");
            }

            // Generate secret
            var secret = Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));
            var qrCodeUrl = $"otpauth://totp/LISA AI:{user.Email}?secret={secret}&issuer=LISA AI";

            // Generate backup codes
            var backupCodes = GenerateBackupCodes();

            // Return the QR code URL - clients can generate QR codes themselves
            return new TwoFactorSetupResult(true, secret, qrCodeUrl, backupCodes, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up 2FA for user {UserId}", userId);
            return new TwoFactorSetupResult(false, null, null, null, "An error occurred");
        }
    }

    public async Task<bool> EnableTwoFactorAsync(Guid userId, string verificationCode)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            // TODO: Get the secret from temporary storage
            // For now, assume it's valid
            var secret = Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));
            
            if (ValidateTwoFactorCode(secret, verificationCode))
            {
                user.EnableTwoFactor(secret);
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling 2FA for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> DisableTwoFactorAsync(Guid userId, string password)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            if (BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                user.DisableTwoFactor();
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling 2FA for user {UserId}", userId);
            return false;
        }
    }

    public async Task<UserProfileDto?> GetUserProfileAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.ClubRoles)
            .FirstOrDefaultAsync(u => u.Id.Value == userId);

        return user != null ? MapToUserProfileDto(user) : null;
    }

    public async Task<bool> UpdateUserProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            // TODO: Add update methods to User entity
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
            return false;
        }
    }

    private bool ValidateTwoFactorCode(string secret, string code)
    {
        try
        {
            var secretBytes = Base32Encoding.ToBytes(secret);
            var totp = new Totp(secretBytes);
            return totp.VerifyTotp(code, out _);
        }
        catch
        {
            return false;
        }
    }

    private string[] GenerateBackupCodes()
    {
        var codes = new string[10];
        var random = new Random();
        
        for (int i = 0; i < codes.Length; i++)
        {
            codes[i] = random.Next(100000, 999999).ToString();
        }
        
        return codes;
    }

    private UserProfileDto MapToUserProfileDto(User user)
    {
        var clubRoles = user.ClubRoles.Select(cr => new ClubRoleDto(
            cr.ClubId,
            $"Club {cr.ClubId}", // TODO: Get actual club name
            cr.Role.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim()).ToList(),
            cr.AssignedAt
        )).ToList();

        return new UserProfileDto(
            user.Id.Value,
            user.Email,
            user.Username,
            user.FirstName,
            user.LastName,
            user.PreferredLanguage,
            user.IsActive,
            user.EmailVerified,
            user.TwoFactorEnabled,
            user.CreatedAt,
            user.LastLoginAt,
            clubRoles
        );
    }
} 
namespace AuthService.Application.DTOs;

public record UserProfileDto(
    Guid Id,
    string Email,
    string Username,
    string FirstName,
    string LastName,
    string PreferredLanguage,
    bool IsActive,
    bool EmailVerified,
    bool TwoFactorEnabled,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    List<ClubRoleDto> ClubRoles
);

public record ClubRoleDto(
    Guid ClubId,
    string ClubName,
    List<string> Roles,
    DateTime AssignedAt
);

public record LoginRequest(
    string UsernameOrEmail,
    string Password,
    string? TwoFactorCode = null
);

public record RegisterRequest(
    string Email,
    string Username,
    string Password,
    string FirstName,
    string LastName,
    string PreferredLanguage = "en-US",
    Guid? InitialClubId = null,
    string InitialRole = "Member"
);

public record UpdateProfileRequest(
    string? FirstName,
    string? LastName,
    string? PreferredLanguage
);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);

public record ResetPasswordRequest(
    string Email,
    string ResetCode,
    string NewPassword
);

public record TwoFactorRequest(
    string VerificationCode
); 
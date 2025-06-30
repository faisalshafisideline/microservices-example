using AuthService.Domain.ValueObjects;

namespace AuthService.Domain.Entities;

public class User
{
    public UserId Id { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string Username { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string PreferredLanguage { get; private set; } = "en-US";
    public bool IsActive { get; private set; } = true;
    public bool EmailVerified { get; private set; } = false;
    public bool TwoFactorEnabled { get; private set; } = false;
    public string? TwoFactorSecret { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Multi-tenant: User can belong to multiple clubs with different roles
    public List<UserClubRole> ClubRoles { get; private set; } = new();

    private User() { } // EF Core

    public User(
        string email,
        string username,
        string passwordHash,
        string firstName,
        string lastName,
        string preferredLanguage = "en-US")
    {
        Id = UserId.New();
        Email = email.ToLowerInvariant();
        Username = username.ToLowerInvariant();
        PasswordHash = passwordHash;
        FirstName = firstName;
        LastName = lastName;
        PreferredLanguage = preferredLanguage;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdatePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void VerifyEmail()
    {
        EmailVerified = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void EnableTwoFactor(string secret)
    {
        TwoFactorEnabled = true;
        TwoFactorSecret = secret;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DisableTwoFactor()
    {
        TwoFactorEnabled = false;
        TwoFactorSecret = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddClubRole(Guid clubId, string role)
    {
        var existingRole = ClubRoles.FirstOrDefault(cr => cr.ClubId == clubId);
        if (existingRole != null)
        {
            existingRole.UpdateRole(role);
        }
        else
        {
            ClubRoles.Add(new UserClubRole(Id.Value, clubId, role));
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveClubRole(Guid clubId)
    {
        var roleToRemove = ClubRoles.FirstOrDefault(cr => cr.ClubId == clubId);
        if (roleToRemove != null)
        {
            ClubRoles.Remove(roleToRemove);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public List<string> GetRolesForClub(Guid clubId)
    {
        return ClubRoles
            .Where(cr => cr.ClubId == clubId)
            .SelectMany(cr => cr.Role.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .Select(r => r.Trim())
            .ToList();
    }

    public bool HasRoleInClub(Guid clubId, string role)
    {
        return GetRolesForClub(clubId).Contains(role, StringComparer.OrdinalIgnoreCase);
    }
}

public class UserClubRole
{
    public Guid UserId { get; private set; }
    public Guid ClubId { get; private set; }
    public string Role { get; private set; } = null!; // Comma-separated roles: "Admin,Manager,Member"
    public DateTime AssignedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private UserClubRole() { } // EF Core

    public UserClubRole(Guid userId, Guid clubId, string role)
    {
        UserId = userId;
        ClubId = clubId;
        Role = role;
        AssignedAt = DateTime.UtcNow;
    }

    public void UpdateRole(string newRole)
    {
        Role = newRole;
        UpdatedAt = DateTime.UtcNow;
    }
} 
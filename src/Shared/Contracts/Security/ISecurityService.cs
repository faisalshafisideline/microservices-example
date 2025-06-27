namespace Shared.Contracts.Security;

/// <summary>
/// Advanced security service for encryption and data protection
/// </summary>
public interface ISecurityService
{
    Task<string> EncryptSensitiveDataAsync(string data, string? keyId = null);
    Task<string> DecryptSensitiveDataAsync(string encryptedData, string? keyId = null);
    Task<string> HashPasswordAsync(string password);
    Task<bool> VerifyPasswordAsync(string password, string hash);
    Task<string> GenerateSecureTokenAsync(int length = 32);
    Task<bool> ValidateTokenAsync(string token, string expectedHash);
}

/// <summary>
/// Rate limiting service
/// </summary>
public interface IRateLimitingService
{
    Task<RateLimitResult> CheckRateLimitAsync(string key, int maxRequests, TimeSpan window, CancellationToken cancellationToken = default);
    Task<RateLimitResult> CheckUserRateLimitAsync(string userId, string operation, CancellationToken cancellationToken = default);
    Task<RateLimitResult> CheckIpRateLimitAsync(string ipAddress, CancellationToken cancellationToken = default);
    Task ResetRateLimitAsync(string key, CancellationToken cancellationToken = default);
}

public class RateLimitResult
{
    public bool IsAllowed { get; set; }
    public int RemainingRequests { get; set; }
    public TimeSpan RetryAfter { get; set; }
    public string? ReasonPhrase { get; set; }
}

/// <summary>
/// Audit logging service
/// </summary>
public interface IAuditService
{
    Task LogUserActionAsync(string userId, string action, string resource, Dictionary<string, object>? metadata = null);
    Task LogSecurityEventAsync(SecurityEventType eventType, string description, string? userId = null, Dictionary<string, object>? metadata = null);
    Task LogDataAccessAsync(string userId, string dataType, string operation, string resourceId);
    Task LogAuthenticationEventAsync(string userId, bool success, string? provider = null, string? ipAddress = null);
    Task<IEnumerable<AuditEntry>> GetAuditTrailAsync(string? userId = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
}

public enum SecurityEventType
{
    Login,
    Logout,
    PasswordChange,
    PermissionEscalation,
    SuspiciousActivity,
    DataBreach,
    UnauthorizedAccess,
    SystemCompromise
}

public class AuditEntry
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public string? CorrelationId { get; set; }
}

/// <summary>
/// Permission-based authorization service
/// </summary>
public interface IPermissionService
{
    Task<bool> HasPermissionAsync(string userId, string resource, string action, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetRolePermissionsAsync(string role, CancellationToken cancellationToken = default);
    Task GrantPermissionAsync(string userId, string resource, string action, CancellationToken cancellationToken = default);
    Task RevokePermissionAsync(string userId, string resource, string action, CancellationToken cancellationToken = default);
}

/// <summary>
/// Data classification and protection service
/// </summary>
public interface IDataProtectionService
{
    Task<string> ClassifyDataAsync(string data);
    Task<string> ApplyDataMaskingAsync(string data, DataSensitivityLevel level);
    Task<bool> ValidateDataAccessAsync(string userId, string dataType, DataSensitivityLevel level);
    Task LogDataAccessAsync(string userId, string dataType, string operation, DataSensitivityLevel level);
}

public enum DataSensitivityLevel
{
    Public,
    Internal,
    Confidential,
    Restricted,
    TopSecret
}

/// <summary>
/// Threat detection service
/// </summary>
public interface IThreatDetectionService
{
    Task<ThreatAssessment> AssessRequestAsync(string userId, string operation, string? ipAddress = null, Dictionary<string, object>? context = null);
    Task ReportSuspiciousActivityAsync(string userId, string activity, string? evidence = null);
    Task<bool> IsUserBlockedAsync(string userId, CancellationToken cancellationToken = default);
    Task BlockUserAsync(string userId, TimeSpan? duration = null, string? reason = null);
}

public class ThreatAssessment
{
    public ThreatLevel Level { get; set; }
    public string? Description { get; set; }
    public IEnumerable<string> Indicators { get; set; } = Array.Empty<string>();
    public bool ShouldBlock { get; set; }
    public TimeSpan? BlockDuration { get; set; }
}

public enum ThreatLevel
{
    None,
    Low,
    Medium,
    High,
    Critical
} 
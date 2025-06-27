namespace Shared.Contracts.UserContext;

/// <summary>
/// Constants for user context propagation headers
/// </summary>
public static class UserContextConstants
{
    // HTTP Headers
    public const string UserIdHeader = "X-User-Id";
    public const string UsernameHeader = "X-Username";
    public const string RolesHeader = "X-User-Roles";
    public const string CorrelationIdHeader = "X-Correlation-Id";
    public const string TenantIdHeader = "X-Tenant-Id";
    public const string ClaimsHeader = "X-User-Claims";
    public const string TimestampHeader = "X-User-Context-Timestamp";

    // gRPC Metadata Keys (lowercase as per gRPC convention)
    public const string GrpcUserIdKey = "x-user-id";
    public const string GrpcUsernameKey = "x-username";
    public const string GrpcRolesKey = "x-user-roles";
    public const string GrpcCorrelationIdKey = "x-correlation-id";
    public const string GrpcTenantIdKey = "x-tenant-id";
    public const string GrpcClaimsKey = "x-user-claims";
    public const string GrpcTimestampKey = "x-user-context-timestamp";

    // RabbitMQ Message Headers
    public const string RabbitUserIdKey = "user-id";
    public const string RabbitUsernameKey = "username";
    public const string RabbitRolesKey = "user-roles";
    public const string RabbitCorrelationIdKey = "correlation-id";
    public const string RabbitTenantIdKey = "tenant-id";
    public const string RabbitClaimsKey = "user-claims";
    public const string RabbitTimestampKey = "user-context-timestamp";

    // Separators
    public const string RolesSeparator = ",";
    public const string ClaimsKeyValueSeparator = "=";
    public const string ClaimsPairSeparator = ";";
} 
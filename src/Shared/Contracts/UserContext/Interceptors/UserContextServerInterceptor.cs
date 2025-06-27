using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Shared.Contracts.UserContext.Interceptors;

/// <summary>
/// gRPC server interceptor that extracts user context from incoming calls
/// </summary>
public class UserContextServerInterceptor : Interceptor
{
    private readonly IUserContextAccessor _userContextAccessor;
    private readonly ILogger<UserContextServerInterceptor> _logger;

    public UserContextServerInterceptor(
        IUserContextAccessor userContextAccessor,
        ILogger<UserContextServerInterceptor> logger)
    {
        _userContextAccessor = userContextAccessor;
        _logger = logger;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        ExtractAndSetUserContext(context);

        try
        {
            return await continuation(request, context);
        }
        finally
        {
            _userContextAccessor.ClearContext();
        }
    }

    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        ExtractAndSetUserContext(context);

        try
        {
            return await continuation(requestStream, context);
        }
        finally
        {
            _userContextAccessor.ClearContext();
        }
    }

    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        ExtractAndSetUserContext(context);

        try
        {
            await continuation(request, responseStream, context);
        }
        finally
        {
            _userContextAccessor.ClearContext();
        }
    }

    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        ExtractAndSetUserContext(context);

        try
        {
            await continuation(requestStream, responseStream, context);
        }
        finally
        {
            _userContextAccessor.ClearContext();
        }
    }

    private void ExtractAndSetUserContext(ServerCallContext context)
    {
        try
        {
            var userContext = ExtractUserContextFromMetadata(context.RequestHeaders);
            _userContextAccessor.SetContext(userContext);

            _logger.LogDebug("Extracted user context from gRPC call: {UserContext}", userContext);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract user context from gRPC call, using empty context");
            _userContextAccessor.SetContext(UserContext.Empty);
        }
    }

    private static UserContext ExtractUserContextFromMetadata(Metadata headers)
    {
        var userId = GetMetadataValue(headers, UserContextConstants.GrpcUserIdKey);
        var username = GetMetadataValue(headers, UserContextConstants.GrpcUsernameKey);
        var rolesString = GetMetadataValue(headers, UserContextConstants.GrpcRolesKey);
        var correlationId = GetMetadataValue(headers, UserContextConstants.GrpcCorrelationIdKey) 
                           ?? Guid.NewGuid().ToString();
        var tenantId = GetMetadataValue(headers, UserContextConstants.GrpcTenantIdKey);
        var claimsString = GetMetadataValue(headers, UserContextConstants.GrpcClaimsKey);
        var timestampString = GetMetadataValue(headers, UserContextConstants.GrpcTimestampKey);

        var roles = UserContextSerializer.DeserializeRoles(rolesString);
        var claims = UserContextSerializer.DeserializeClaims(claimsString);
        var timestamp = UserContextSerializer.DeserializeTimestamp(timestampString);

        return new UserContext
        {
            UserId = userId,
            Username = username,
            Roles = roles,
            CorrelationId = correlationId,
            TenantId = tenantId,
            Claims = claims,
            Timestamp = timestamp
        };
    }

    private static string? GetMetadataValue(Metadata headers, string key)
    {
        var entry = headers.FirstOrDefault(h => 
            string.Equals(h.Key, key, StringComparison.OrdinalIgnoreCase));
        
        return entry?.Value;
    }
} 
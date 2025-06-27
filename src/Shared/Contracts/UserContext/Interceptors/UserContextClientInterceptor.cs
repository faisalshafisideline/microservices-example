using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Shared.Contracts.UserContext.Interceptors;

/// <summary>
/// gRPC client interceptor that injects user context into outgoing calls
/// </summary>
public class UserContextClientInterceptor : Interceptor
{
    private readonly IUserContextAccessor _userContextAccessor;
    private readonly ILogger<UserContextClientInterceptor> _logger;

    public UserContextClientInterceptor(
        IUserContextAccessor userContextAccessor,
        ILogger<UserContextClientInterceptor> logger)
    {
        _userContextAccessor = userContextAccessor;
        _logger = logger;
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var headers = InjectUserContext(context.Options.Headers);
        var newOptions = context.Options.WithHeaders(headers);
        var newContext = new ClientInterceptorContext<TRequest, TResponse>(
            context.Method, context.Host, newOptions);

        return continuation(request, newContext);
    }

    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        var headers = InjectUserContext(context.Options.Headers);
        var newOptions = context.Options.WithHeaders(headers);
        var newContext = new ClientInterceptorContext<TRequest, TResponse>(
            context.Method, context.Host, newOptions);

        return continuation(newContext);
    }

    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        var headers = InjectUserContext(context.Options.Headers);
        var newOptions = context.Options.WithHeaders(headers);
        var newContext = new ClientInterceptorContext<TRequest, TResponse>(
            context.Method, context.Host, newOptions);

        return continuation(request, newContext);
    }

    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        var headers = InjectUserContext(context.Options.Headers);
        var newOptions = context.Options.WithHeaders(headers);
        var newContext = new ClientInterceptorContext<TRequest, TResponse>(
            context.Method, context.Host, newOptions);

        return continuation(newContext);
    }

    private Metadata InjectUserContext(Metadata? originalHeaders)
    {
        var headers = originalHeaders ?? new Metadata();
        var userContext = _userContextAccessor.GetCurrentOrEmpty();

        try
        {
            // Add user context to gRPC metadata
            if (!string.IsNullOrEmpty(userContext.UserId))
                headers.Add(UserContextConstants.GrpcUserIdKey, userContext.UserId);

            if (!string.IsNullOrEmpty(userContext.Username))
                headers.Add(UserContextConstants.GrpcUsernameKey, userContext.Username);

            if (userContext.Roles.Any())
                headers.Add(UserContextConstants.GrpcRolesKey, UserContextSerializer.SerializeRoles(userContext.Roles));

            headers.Add(UserContextConstants.GrpcCorrelationIdKey, userContext.CorrelationId);

            if (!string.IsNullOrEmpty(userContext.TenantId))
                headers.Add(UserContextConstants.GrpcTenantIdKey, userContext.TenantId);

            if (userContext.Claims.Any())
                headers.Add(UserContextConstants.GrpcClaimsKey, UserContextSerializer.SerializeClaims(userContext.Claims));

            headers.Add(UserContextConstants.GrpcTimestampKey, UserContextSerializer.SerializeTimestamp(userContext.Timestamp));

            _logger.LogDebug("Injected user context into gRPC call: {CorrelationId}", userContext.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to inject user context into gRPC call");
        }

        return headers;
    }
} 
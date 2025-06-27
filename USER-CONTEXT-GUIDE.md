# User Context Propagation Guide

This guide explains how to implement and use the **User Context Propagation** system across your .NET microservices architecture.

## 🎯 Overview

The User Context system provides a robust mechanism to capture, propagate, and consume user information (user ID, roles, correlation ID, etc.) across:

- **HTTP requests** (via YARP API Gateway)
- **gRPC calls** between services
- **RabbitMQ messages** for async communication

## 📦 Core Components

### UserContext Model

```csharp
public class UserContext
{
    public string? UserId { get; init; }
    public string? Username { get; init; }
    public IReadOnlyList<string> Roles { get; init; }
    public string CorrelationId { get; init; }
    public string? TenantId { get; init; }
    public IReadOnlyDictionary<string, string> Claims { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}
```

### IUserContextAccessor

```csharp
public interface IUserContextAccessor
{
    UserContext? Current { get; }
    void SetContext(UserContext context);
    void ClearContext();
    UserContext GetCurrentOrEmpty();
    UserContext GetRequiredContext();
}
```

## 🚀 Quick Start

### 1. Add User Context to Services

In each service's `Program.cs`:

```csharp
using Shared.Contracts.UserContext.Extensions;

// Add user context services
builder.Services.AddUserContext();

// For services with gRPC
builder.Services.AddUserContextGrpcInterceptors();
```

### 2. Configure HTTP Middleware

```csharp
// Add middleware (after authentication, before business logic)
app.UseUserContext();
```

### 3. Configure gRPC Services

**For gRPC Server:**
```csharp
builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<UserContextServerInterceptor>();
});
```

**For gRPC Client:**
```csharp
builder.Services.AddGrpcClient<ArticleService.ArticleServiceClient>(options =>
{
    options.Address = new Uri("https://localhost:7001");
})
.AddInterceptor<UserContextClientInterceptor>();
```

### 4. Configure MassTransit (RabbitMQ)

```csharp
builder.Services.AddMassTransit(x =>
{
    x.ConfigureUserContextPropagation(); // Add filters
    
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h => {
            h.Username("guest");
            h.Password("guest");
        });
        
        cfg.ConfigureUserContextPropagation(context); // Add to bus
        cfg.ConfigureEndpoints(context);
    });
});
```

## 📋 Implementation Examples

### 1. API Gateway Setup

The API Gateway automatically extracts user context from authentication and adds headers:

```csharp
// In ApiGateway/Program.cs
builder.Services.AddUserContext();

// Add middleware after authentication
app.UseAuthentication();
app.UseMiddleware<UserContextEnrichmentMiddleware>();
app.UseAuthorization();
```

### 2. Using Context in Business Logic

```csharp
public class ArticleService
{
    private readonly IUserContextAccessor _userContextAccessor;

    public ArticleService(IUserContextAccessor userContextAccessor)
    {
        _userContextAccessor = userContextAccessor;
    }

    public async Task<Article> CreateArticleAsync(CreateArticleRequest request)
    {
        var userContext = _userContextAccessor.GetRequiredContext();
        
        // Use context for business logic
        var article = new Article
        {
            Title = request.Title,
            Content = request.Content,
            AuthorId = userContext.UserId,
            CreatedBy = userContext.Username,
            CorrelationId = userContext.CorrelationId
        };

        // Context is automatically propagated to:
        // - Database operations (via interceptors)
        // - gRPC calls (via client interceptor)
        // - Published events (via MassTransit filters)
        
        return article;
    }
}
```

### 3. gRPC Service Implementation

```csharp
public class ArticleGrpcService : ArticleService.ArticleServiceBase
{
    private readonly IUserContextAccessor _userContextAccessor;

    public override async Task<GetArticleResponse> GetArticle(
        GetArticleRequest request, 
        ServerCallContext context)
    {
        // Context is automatically extracted by UserContextServerInterceptor
        var userContext = _userContextAccessor.Current;
        
        _logger.LogInformation("Processing gRPC request for user: {UserId}, Correlation: {CorrelationId}",
            userContext?.UserId, userContext?.CorrelationId);

        // Business logic here...
    }
}
```

### 4. RabbitMQ Consumer

```csharp
public class ArticleCreatedEventConsumer : IConsumer<ArticleCreatedEvent>
{
    private readonly IUserContextAccessor _userContextAccessor;

    public async Task Consume(ConsumeContext<ArticleCreatedEvent> context)
    {
        // Context is automatically extracted by UserContextConsumeFilter
        var userContext = _userContextAccessor.Current;
        
        _logger.LogInformation("Processing event for user: {UserId}, Correlation: {CorrelationId}",
            userContext?.UserId, userContext?.CorrelationId);

        // Process the event...
    }
}
```

### 5. Publishing Events with Context

```csharp
public class ArticleCommandHandler
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IUserContextAccessor _userContextAccessor;

    public async Task Handle(CreateArticleCommand command)
    {
        // Create article...
        
        // Publish event - context is automatically injected by UserContextPublishFilter
        await _publishEndpoint.Publish(new ArticleCreatedEvent
        {
            ArticleId = article.Id,
            Title = article.Title,
            AuthorId = article.AuthorId
        });
    }
}
```

## 🔧 Configuration Details

### HTTP Headers

The system uses these headers for HTTP propagation:

```
X-User-Id: user123
X-Username: john.doe
X-User-Roles: Admin,Author
X-Correlation-Id: 12345678-1234-1234-1234-123456789012
X-Tenant-Id: tenant1
X-User-Claims: claim1=value1;claim2=value2
X-User-Context-Timestamp: 2024-01-01T12:00:00.000Z
```

### gRPC Metadata

For gRPC, the same information is transmitted via metadata with lowercase keys:

```
x-user-id: user123
x-username: john.doe
x-user-roles: Admin,Author
x-correlation-id: 12345678-1234-1234-1234-123456789012
```

### RabbitMQ Headers

For RabbitMQ messages, context is stored in message headers:

```
user-id: user123
username: john.doe
user-roles: Admin,Author
correlation-id: 12345678-1234-1234-1234-123456789012
```

## 🧪 Testing Context Propagation

### 1. Test HTTP to gRPC Propagation

```bash
# Call API Gateway with authentication
curl -X GET "http://localhost:5000/api/articles/123" \
  -H "Authorization: Basic YWRtaW46c3VwZXJzZWNyZXQ=" \
  -H "X-Correlation-Id: test-correlation-123"
```

### 2. Verify Context in Logs

Look for log entries showing context propagation:

```
[INFO] User context enriched for gateway request: User: admin (admin), Roles: [Admin, Reporter, User], CorrelationId: test-correlation-123
[INFO] Injected user context into gRPC call: test-correlation-123
[INFO] Extracted user context from gRPC call: User: admin (admin), Roles: [Admin, Reporter, User], CorrelationId: test-correlation-123
```

## 🎛️ Advanced Configuration

### Custom Context Enrichment

Create custom middleware to add tenant-specific information:

```csharp
public class TenantContextMiddleware
{
    public async Task InvokeAsync(HttpContext context, IUserContextAccessor userContextAccessor)
    {
        var currentContext = userContextAccessor.Current;
        if (currentContext != null)
        {
            // Extract tenant from subdomain or header
            var tenantId = ExtractTenantId(context);
            
            var enrichedContext = currentContext with { TenantId = tenantId };
            userContextAccessor.SetContext(enrichedContext);
        }

        await _next(context);
    }
}
```

### Custom Claims Processing

```csharp
public class CustomUserContextMiddleware : UserContextMiddleware
{
    protected override UserContext ExtractUserContext(HttpContext context)
    {
        var baseContext = base.ExtractUserContext(context);
        
        // Add custom claims processing
        var customClaims = new Dictionary<string, string>(baseContext.Claims)
        {
            ["department"] = context.User.FindFirst("department")?.Value ?? "unknown",
            ["location"] = context.User.FindFirst("location")?.Value ?? "remote"
        };

        return baseContext.WithClaims(customClaims);
    }
}
```

## 🔍 Troubleshooting

### Common Issues

1. **Context is null in business logic**
   - Ensure middleware is registered after authentication
   - Verify UserContext services are registered in DI

2. **gRPC context not propagating**
   - Check that both client and server interceptors are registered
   - Verify interceptor order in gRPC configuration

3. **RabbitMQ context missing**
   - Ensure MassTransit filters are configured
   - Check that both publish and consume filters are added

### Debug Logging

Enable debug logging to trace context flow:

```json
{
  "Logging": {
    "LogLevel": {
      "Shared.Contracts.UserContext": "Debug"
    }
  }
}
```

## 📚 Best Practices

1. **Always use GetCurrentOrEmpty()** for optional context
2. **Use GetRequiredContext()** when context is mandatory
3. **Set context early** in the request pipeline
4. **Clear context** in finally blocks for long-running operations
5. **Log correlation IDs** for distributed tracing
6. **Validate sensitive operations** using context roles/claims

## 🔐 Security Considerations

- Context headers are automatically added by the gateway
- Don't trust context headers from external clients
- Validate permissions using context in business logic
- Consider encrypting sensitive context data
- Audit context usage for compliance

## 📖 Next Steps

1. Implement custom context enrichment for your domain
2. Add distributed tracing using correlation IDs
3. Implement audit logging using user context
4. Create authorization policies based on context
5. Add monitoring and alerting for context failures 
# Apollo User Context Propagation Guide

This guide explains how to implement and use the **User Context Propagation** system across Apollo's multi-tenant sports club management microservices.

## 🎯 Overview

The User Context system provides a robust mechanism to capture, propagate, and consume user information (user ID, club roles, correlation ID, etc.) across Apollo's microservices:

- **HTTP requests** (via YARP API Gateway)
- **gRPC calls** between services
- **RabbitMQ messages** for async communication
- **Multi-tenant club data** separation

## 📦 Core Components

### UserContext Model

```csharp
public class UserContext
{
    public string? UserId { get; init; }
    public string? Username { get; init; }
    public IReadOnlyList<string> Roles { get; init; }
    public string CorrelationId { get; init; }
    public string? ClubId { get; init; }        // Apollo: Current club context
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

## 🚀 Quick Start for Apollo Services

### 1. Add User Context to Apollo Services

In each Apollo service's `Program.cs`:

```csharp
using Shared.Contracts.UserContext.Extensions;

// Add user context services for Apollo
builder.Services.AddUserContext();

// For services with gRPC (AuthService, ClubService, MemberService)
builder.Services.AddUserContextGrpcInterceptors();
```

### 2. Configure HTTP Middleware

```csharp
// Add middleware (after authentication, before business logic)
app.UseUserContext();
```

### 3. Configure gRPC Services

**For gRPC Server (AuthService, ClubService, MemberService):**
```csharp
builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<UserContextServerInterceptor>();
});
```

**For gRPC Client:**
```csharp
// Example: MemberService calling AuthService
builder.Services.AddGrpcClient<AuthService.AuthServiceClient>(options =>
{
    options.Address = new Uri("https://localhost:8081");
})
.AddInterceptor<UserContextClientInterceptor>();
```

### 4. Configure MassTransit for Apollo Events

```csharp
builder.Services.AddMassTransit(x =>
{
    x.ConfigureUserContextPropagation(); // Add Apollo context filters
    
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h => {
            h.Username("admin");
            h.Password("admin");
        });
        
        cfg.ConfigureUserContextPropagation(context);
        cfg.ConfigureEndpoints(context);
    });
});
```

## 📋 Apollo Implementation Examples

### 1. API Gateway Setup for Apollo

The API Gateway automatically extracts user context from JWT tokens and adds club context:

```csharp
// In ApiGateway/Program.cs
builder.Services.AddUserContext();

// Add middleware after JWT authentication
app.UseAuthentication();
app.UseMiddleware<UserContextEnrichmentMiddleware>();
app.UseAuthorization();
```

### 2. Multi-Tenant Club Context in Business Logic

```csharp
public class ClubService
{
    private readonly IUserContextAccessor _userContextAccessor;

    public ClubService(IUserContextAccessor userContextAccessor)
    {
        _userContextAccessor = userContextAccessor;
    }

    public async Task<Club> CreateClubAsync(CreateClubRequest request)
    {
        var userContext = _userContextAccessor.GetRequiredContext();
        
        // Apollo: Use context for multi-tenant operations
        var club = new Club(
            name: request.Name,
            code: request.Code,
            country: request.Country,
            primaryContactEmail: request.PrimaryContactEmail,
            primaryContactName: request.PrimaryContactName,
            address: request.Address,
            createdBy: userContext.UserId // Audit trail
        );

        // Context is automatically propagated to:
        // - Database operations (with club isolation)
        // - gRPC calls to other Apollo services
        // - Published events (ClubCreatedEvent)
        
        return club;
    }
}
```

### 3. Member Service with Club Context

```csharp
public class MemberService
{
    private readonly IUserContextAccessor _userContextAccessor;

    public async Task<Member> AddMemberAsync(AddMemberRequest request)
    {
        var userContext = _userContextAccessor.GetRequiredContext();
        
        // Apollo: Ensure user has access to the club
        if (userContext.ClubId != request.ClubId)
        {
            throw new UnauthorizedAccessException("User not authorized for this club");
        }

        var member = new Member(
            clubId: Guid.Parse(request.ClubId),
            userId: Guid.Parse(request.UserId),
            memberNumber: request.MemberNumber,
            firstName: request.FirstName,
            lastName: request.LastName,
            email: request.Email,
            membershipType: request.MembershipType,
            membershipFee: request.MembershipFee,
            currency: request.Currency,
            createdBy: userContext.UserId
        );

        return member;
    }
}
```

### 4. Apollo gRPC Service Implementation

```csharp
public class AuthGrpcService : AuthService.AuthServiceBase
{
    private readonly IUserContextAccessor _userContextAccessor;

    public override async Task<ValidateTokenResponse> ValidateToken(
        ValidateTokenRequest request, 
        ServerCallContext context)
    {
        // Context is automatically extracted by UserContextServerInterceptor
        var userContext = _userContextAccessor.Current;
        
        _logger.LogInformation("Validating token for user: {UserId}, Club: {ClubId}, Correlation: {CorrelationId}",
            userContext?.UserId, userContext?.ClubId, userContext?.CorrelationId);

        // Apollo: Validate JWT and return user with club roles
        var user = await _authService.ValidateTokenAsync(request.Token);
        
        return new ValidateTokenResponse
        {
            IsValid = user != null,
            User = MapToGrpcUser(user),
            Permissions = { user?.GetClubPermissions(userContext?.ClubId) ?? [] }
        };
    }
}
```

### 5. Apollo Event Consumer with Club Context

```csharp
public class MemberJoinedEventConsumer : IConsumer<MemberJoinedEvent>
{
    private readonly IUserContextAccessor _userContextAccessor;
    private readonly ICommunicationService _communicationService;

    public async Task Consume(ConsumeContext<MemberJoinedEvent> context)
    {
        // Context is automatically extracted by UserContextConsumeFilter
        var userContext = _userContextAccessor.Current;
        
        _logger.LogInformation("Processing member joined event for Club: {ClubId}, Member: {MemberId}, Correlation: {CorrelationId}",
            context.Message.ClubId, context.Message.MemberId, userContext?.CorrelationId);

        // Apollo: Send welcome notification to new member
        await _communicationService.SendWelcomeNotificationAsync(
            clubId: context.Message.ClubId,
            memberId: context.Message.MemberId,
            memberEmail: context.Message.MemberEmail
        );
    }
}
```

### 6. Publishing Apollo Events with Context

```csharp
public class ClubService
{
    private readonly IPublishEndpoint _publishEndpoint;

    public async Task CreateClubAsync(CreateClubRequest request)
    {
        // Create club logic...
        
        // Apollo: Publish club created event with full context
        await _publishEndpoint.Publish(new ClubCreatedEvent
        {
            ClubId = club.Id,
            ClubName = club.Name,
            ClubCode = club.Code,
            CreatedBy = club.CreatedBy,
            CreatedAt = club.CreatedAt
        });
        // User context is automatically added by UserContextPublishFilter
    }
}
```

## 🏢 Apollo Multi-Tenant Scenarios

### 1. Club Data Isolation

```csharp
public class MemberRepository
{
    private readonly IUserContextAccessor _userContextAccessor;

    public async Task<List<Member>> GetMembersAsync()
    {
        var userContext = _userContextAccessor.GetRequiredContext();
        
        // Apollo: Always filter by club context for data isolation
        return await _dbContext.Members
            .Where(m => m.ClubId == Guid.Parse(userContext.ClubId))
            .ToListAsync();
    }
}
```

### 2. Club Role-Based Authorization

```csharp
public class ClubAuthorizationService
{
    private readonly IUserContextAccessor _userContextAccessor;

    public async Task<bool> CanManageMembersAsync()
    {
        var userContext = _userContextAccessor.GetRequiredContext();
        
        // Apollo: Check if user has manager role in current club
        return userContext.Roles.Contains($"Club:{userContext.ClubId}:Manager") ||
               userContext.Roles.Contains($"Club:{userContext.ClubId}:Admin");
    }
}
```

### 3. Cross-Service Club Validation

```csharp
public class MemberService
{
    private readonly AuthService.AuthServiceClient _authClient;

    public async Task AddMemberAsync(AddMemberRequest request)
    {
        // Apollo: Validate user has access to the club via AuthService
        var validation = await _authClient.ValidateClubAccessAsync(new ValidateClubAccessRequest
        {
            ClubId = request.ClubId,
            UserId = _userContextAccessor.Current.UserId
        });

        if (!validation.HasAccess)
        {
            throw new UnauthorizedAccessException("User not authorized for this club");
        }

        // Proceed with adding member...
    }
}
```

## 🔍 Debugging Apollo User Context

### 1. Logging Context Information

```csharp
public class ApolloContextLoggingMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var userContext = _userContextAccessor.Current;
        
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["UserId"] = userContext?.UserId ?? "Anonymous",
            ["ClubId"] = userContext?.ClubId ?? "None",
            ["CorrelationId"] = userContext?.CorrelationId ?? Guid.NewGuid().ToString()
        });

        await next(context);
    }
}
```

### 2. Health Check with Context

```csharp
public class ApolloContextHealthCheck : IHealthCheck
{
    private readonly IUserContextAccessor _userContextAccessor;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var userContext = _userContextAccessor.Current;
        
        var data = new Dictionary<string, object>
        {
            ["HasUserContext"] = userContext != null,
            ["UserId"] = userContext?.UserId ?? "None",
            ["ClubId"] = userContext?.ClubId ?? "None"
        };

        return Task.FromResult(HealthCheckResult.Healthy("Apollo User Context is working", data));
    }
}
```

## 🎯 Apollo Best Practices

### 1. Always Validate Club Access
```csharp
// Always check club access in service operations
var userContext = _userContextAccessor.GetRequiredContext();
if (string.IsNullOrEmpty(userContext.ClubId))
{
    throw new InvalidOperationException("Club context is required");
}
```

### 2. Use Club-Scoped Queries
```csharp
// Always include club filtering in database queries
var members = await _dbContext.Members
    .Where(m => m.ClubId == Guid.Parse(userContext.ClubId))
    .ToListAsync();
```

### 3. Include Context in Events
```csharp
// Apollo events should include club context
public class MemberJoinedEvent
{
    public Guid ClubId { get; set; }      // Required for multi-tenancy
    public Guid MemberId { get; set; }
    public string MemberEmail { get; set; }
    public DateTime JoinedAt { get; set; }
}
```

## 🔒 Security Considerations for Apollo

1. **Club Isolation**: Always validate club access before operations
2. **Role Validation**: Check club-specific roles for authorization
3. **Data Filtering**: Never expose data across club boundaries
4. **Audit Trails**: Include user and club context in all operations
5. **Token Validation**: Ensure JWT tokens contain valid club claims

---

**Apollo** - Empowering sports clubs with secure, multi-tenant technology 🚀

For support: [support@apollo-sports.com](mailto:support@apollo-sports.com) 
# Apollo API Gateway Implementation Guide

This guide provides detailed information about the YARP-based API Gateway with JWT authentication implemented in Apollo's sports club management microservices architecture.

## 🎯 Overview

The Apollo API Gateway serves as the single entry point for all client requests, providing:
- **JWT Authentication**: Token-based auth with Apollo AuthService integration
- **Multi-Tenant Authorization**: Club-based access control
- **Request Routing**: YARP reverse proxy to Apollo backend services
- **User Context Propagation**: Seamless context flow across services
- **Observability**: Request logging and health monitoring
- **Cross-cutting Concerns**: CORS, security headers, rate limiting

## 🏗️ Apollo Architecture Components

```
┌─────────────┐    ┌──────────────────┐    ┌─────────────────────┐
│   Clients   │───▶│  Apollo Gateway  │───▶│  Apollo Services    │
│             │    │                  │    │                     │
│ • Web Apps  │    │ • JWT Auth       │    │ • 🔐 AuthService    │
│ • Mobile    │    │ • Club Context   │    │ • 🏢 ClubService    │
│ • Admin     │    │ • Routing (YARP) │    │ • 👥 MemberService  │
│ • APIs      │    │ • User Context   │    │ • 📧 Communication  │
└─────────────┘    │ • Rate Limiting  │    └─────────────────────┘
                   │ • Health Checks  │
                   └──────────────────┘
```

## 🔐 JWT Authentication Implementation

### Apollo AuthService Integration

The Gateway integrates with Apollo's AuthService for token validation:

```csharp
public class ApolloJwtAuthenticationHandler : AuthenticationHandler<JwtBearerOptions>
{
    private readonly AuthService.AuthServiceClient _authClient;
    
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Extract JWT token from Authorization header
        var token = ExtractTokenFromHeader();
        
        // Validate token with Apollo AuthService
        var validation = await _authClient.ValidateTokenAsync(new ValidateTokenRequest
        {
            Token = token
        });
        
        if (!validation.IsValid)
            return AuthenticateResult.Fail("Invalid token");
            
        // Create claims with club roles
        var claims = CreateClaimsFromUser(validation.User);
        var identity = new ClaimsIdentity(claims, "Apollo-JWT");
        
        return AuthenticateResult.Success(new AuthenticationTicket(
            new ClaimsPrincipal(identity), "Apollo-JWT"));
    }
}
```

### Multi-Tenant Club Context

Located in `src/ApiGateway/Middleware/UserContextEnrichmentMiddleware.cs`:

```csharp
public class UserContextEnrichmentMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var user = context.User;
        if (user.Identity?.IsAuthenticated == true)
        {
            // Extract club context from JWT claims or headers
            var clubId = ExtractClubContext(context);
            
            var userContext = new UserContext
            {
                UserId = user.FindFirst("sub")?.Value,
                Username = user.FindFirst("preferred_username")?.Value,
                ClubId = clubId,
                Roles = user.FindAll("role").Select(c => c.Value).ToList(),
                CorrelationId = GetOrCreateCorrelationId(context)
            };
            
            // Set context for downstream services
            _userContextAccessor.SetContext(userContext);
            
            // Add headers for downstream services
            AddUserContextHeaders(context, userContext);
        }
        
        await next(context);
    }
}
```

## 🛡️ Apollo Authorization Policies

### Policy Definitions

Located in `src/ApiGateway/Authorization/Policies.cs`:

```csharp
public static class ApolloPolicies
{
    // System-wide policies
    public const string SystemAdmin = "SystemAdmin";
    public const string AuthenticatedUser = "AuthenticatedUser";
    
    // Club-level policies
    public const string ClubAdmin = "ClubAdmin";
    public const string ClubManager = "ClubManager";
    public const string ClubMember = "ClubMember";
    
    // Service-specific policies
    public const string ManageMembers = "ManageMembers";
    public const string ViewReports = "ViewReports";
    public const string SendNotifications = "SendNotifications";
    
    // Public access
    public const string PublicRead = "PublicRead";
}
```

### Route-Based Authorization

Located in `src/ApiGateway/Middleware/AuthorizationMiddleware.cs`:

```csharp
private static string? DetermineRequiredPolicy(string path, string method)
{
    return path switch
    {
        // Public endpoints
        _ when path.StartsWith("/health") => null,
        _ when path.StartsWith("/swagger") => null,
        _ when path.StartsWith("/api/auth/login") => null,
        _ when path.StartsWith("/api/auth/register") => null,
        
        // Auth service endpoints
        _ when path.StartsWith("/api/auth") => ApolloPolicies.AuthenticatedUser,
        
        // Club management endpoints
        _ when path.StartsWith("/api/clubs") && method == "GET" => ApolloPolicies.ClubMember,
        _ when path.StartsWith("/api/clubs") && method == "POST" => ApolloPolicies.SystemAdmin,
        _ when path.StartsWith("/api/clubs") && method == "PUT" => ApolloPolicies.ClubAdmin,
        
        // Member management endpoints
        _ when path.StartsWith("/api/members") && method == "GET" => ApolloPolicies.ClubMember,
        _ when path.StartsWith("/api/members") && method == "POST" => ApolloPolicies.ManageMembers,
        _ when path.StartsWith("/api/members") && method == "PUT" => ApolloPolicies.ManageMembers,
        
        // Communication endpoints
        _ when path.StartsWith("/api/notifications") => ApolloPolicies.SendNotifications,
        
        // Default
        _ => ApolloPolicies.AuthenticatedUser
    };
}
```

## 🔄 Apollo YARP Configuration

### Route Configuration

Located in `src/ApiGateway/appsettings.json`:

```json
{
  "ReverseProxy": {
    "Routes": {
      "auth-service-route": {
        "ClusterId": "auth-service-cluster",
        "Match": {
          "Path": "/api/auth/{**catch-all}"
        },
        "Transforms": [
          {
            "PathPattern": "/api/auth/{**catch-all}"
          },
          {
            "RequestHeader": "X-User-Context",
            "Append": "{user.context.serialized}"
          }
        ]
      },
      "club-service-route": {
        "ClusterId": "club-service-cluster", 
        "Match": {
          "Path": "/api/clubs/{**catch-all}"
        },
        "Transforms": [
          {
            "PathPattern": "/api/clubs/{**catch-all}"
          },
          {
            "RequestHeader": "X-Club-Id",
            "Append": "{user.claim.club_id}"
          }
        ]
      },
      "member-service-route": {
        "ClusterId": "member-service-cluster",
        "Match": {
          "Path": "/api/members/{**catch-all}"
        },
        "Transforms": [
          {
            "PathPattern": "/api/members/{**catch-all}"
          }
        ]
      },
      "communication-service-route": {
        "ClusterId": "communication-service-cluster",
        "Match": {
          "Path": "/api/notifications/{**catch-all}"
        },
        "Transforms": [
          {
            "PathPattern": "/api/notifications/{**catch-all}"
          }
        ]
      }
    },
    "Clusters": {
      "auth-service-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://localhost:8081/"
          }
        },
        "HealthCheck": {
          "Active": {
            "Enabled": true,
            "Interval": "00:00:30",
            "Path": "/health"
          }
        }
      },
      "club-service-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://localhost:8082/"
          }
        },
        "HealthCheck": {
          "Active": {
            "Enabled": true,
            "Interval": "00:00:30", 
            "Path": "/health"
          }
        }
      },
      "member-service-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://localhost:8083/"
          }
        },
        "HealthCheck": {
          "Active": {
            "Enabled": true,
            "Interval": "00:00:30",
            "Path": "/health"
          }
        }
      },
      "communication-service-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://localhost:8084/"
          }
        },
        "HealthCheck": {
          "Active": {
            "Enabled": true,
            "Interval": "00:00:30",
            "Path": "/health"
          }
        }
      }
    }
  }
}
```

### Key Features

- **Service Discovery**: Route requests to appropriate Apollo services
- **Context Propagation**: Forward user and club context automatically
- **Health Monitoring**: Track all Apollo service health
- **Load Balancing**: Distribute load across service instances
- **Circuit Breaker**: Fail fast when services are unavailable

## 📊 Apollo Gateway Endpoints

### Management Endpoints

Located in `src/ApiGateway/Endpoints/GatewayEndpoints.cs`:

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/gateway` | GET | None | Apollo Gateway information |
| `/api/gateway/user` | GET | Required | Current user & club context |
| `/api/gateway/health` | GET | None | Gateway & services health |
| `/api/gateway/clubs` | GET | Required | User's accessible clubs |
| `/api/gateway/routes` | GET | Admin | Available Apollo routes |
| `/api/gateway/metrics` | GET | Admin | Gateway performance metrics |

### Example Responses

**Apollo Gateway Info** (`/api/gateway`):
```json
{
  "name": "Apollo Sports Club Management Gateway",
  "version": "1.0.0",
  "environment": "Development",
  "authentication": {
    "scheme": "Bearer JWT",
    "provider": "Apollo AuthService",
    "supportedFeatures": ["2FA", "ClubRoles", "MultiTenant"]
  },
  "services": {
    "authService": {
      "path": "/api/auth",
      "health": "Healthy",
      "version": "1.0.0"
    },
    "clubService": {
      "path": "/api/clubs", 
      "health": "Healthy",
      "version": "1.0.0"
    },
    "memberService": {
      "path": "/api/members",
      "health": "Healthy", 
      "version": "1.0.0"
    },
    "communicationService": {
      "path": "/api/notifications",
      "health": "Healthy",
      "version": "1.0.0"
    }
  },
  "features": [
    "Multi-Tenant Club Management",
    "Sports Member Profiles",
    "Real-time Notifications",
    "Subscription Management"
  ]
}
```

**Current User Context** (`/api/gateway/user`):
```json
{
  "userId": "123e4567-e89b-12d3-a456-426614174000",
  "username": "john.doe@fcbarcelona.com",
  "firstName": "John",
  "lastName": "Doe",
  "currentClub": {
    "clubId": "456e7890-e89b-12d3-a456-426614174001",
    "clubName": "FC Barcelona",
    "clubCode": "FC-BARCELONA",
    "roles": ["ClubManager", "Member"],
    "permissions": ["ManageMembers", "ViewReports", "SendNotifications"]
  },
  "accessibleClubs": [
    {
      "clubId": "456e7890-e89b-12d3-a456-426614174001",
      "clubName": "FC Barcelona",
      "roles": ["ClubManager"]
    },
    {
      "clubId": "789e0123-e89b-12d3-a456-426614174002", 
      "clubName": "Real Madrid",
      "roles": ["Member"]
    }
  ],
  "correlationId": "req-123456789",
  "sessionExpiry": "2024-12-07T15:30:00Z"
}
```

## 🔧 Configuration Examples

### JWT Configuration

Located in `src/ApiGateway/Program.cs`:

```csharp
builder.Services.AddAuthentication("Apollo-JWT")
    .AddScheme<JwtBearerOptions, ApolloJwtAuthenticationHandler>("Apollo-JWT", options =>
    {
        options.Authority = "http://localhost:8081"; // Apollo AuthService
        options.RequireHttpsMetadata = false; // Development only
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "Apollo",
            ValidAudience = "Apollo-Services",
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    });
```

### Authorization Policies

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(ApolloPolicies.SystemAdmin, policy =>
        policy.RequireRole("SystemAdmin"));
        
    options.AddPolicy(ApolloPolicies.ClubAdmin, policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("role", "ClubAdmin") ||
            context.User.HasClaim("role", "SystemAdmin")));
            
    options.AddPolicy(ApolloPolicies.ManageMembers, policy =>
        policy.RequireAssertion(context =>
        {
            var clubId = context.Resource as string; // Current club context
            return context.User.HasClaim("club_role", $"{clubId}:Manager") ||
                   context.User.HasClaim("club_role", $"{clubId}:Admin");
        }));
});
```

## 🚀 Apollo Gateway Features

### Rate Limiting

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        httpContext => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? "anonymous",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
            
    // Club-specific rate limiting
    options.AddPolicy("ClubOperations", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.FindFirst("club_id")?.Value ?? "no-club",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 1000,
                Window = TimeSpan.FromMinutes(1)
            }));
});
```

### Circuit Breaker

```csharp
builder.Services.AddHttpClient("apollo-services")
    .AddPolicyHandler(Policy
        .Handle<HttpRequestException>()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 3,
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (exception, duration) =>
                logger.LogWarning("Circuit breaker opened for {Duration}s", duration.TotalSeconds),
            onReset: () =>
                logger.LogInformation("Circuit breaker closed")));
```

### Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddCheck("apollo-gateway", () => HealthCheckResult.Healthy())
    .AddUrlGroup(new Uri("http://localhost:8081/health"), "apollo-auth-service")
    .AddUrlGroup(new Uri("http://localhost:8082/health"), "apollo-club-service") 
    .AddUrlGroup(new Uri("http://localhost:8083/health"), "apollo-member-service")
    .AddUrlGroup(new Uri("http://localhost:8084/health"), "apollo-communication-service");
```

## 🧪 Testing Apollo Gateway

### Authentication Testing

```bash
# Login to get JWT token
curl -X POST http://localhost:8081/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@apollo-sports.com",
    "password": "admin123"
  }'

# Use token with Gateway
curl -X GET http://localhost:8080/api/clubs \
  -H "Authorization: Bearer <jwt-token>"
```

### Multi-Tenant Testing

```bash
# Switch club context
curl -X GET http://localhost:8080/api/members \
  -H "Authorization: Bearer <jwt-token>" \
  -H "X-Club-Id: 456e7890-e89b-12d3-a456-426614174001"
```

### Health Check Testing

```bash
# Gateway health
curl http://localhost:8080/health

# Detailed health with auth
curl http://localhost:8080/api/gateway/health \
  -H "Authorization: Bearer <jwt-token>"
```

## 🔍 Monitoring & Observability

### Request Logging

```csharp
app.Use(async (context, next) =>
{
    var userContext = _userContextAccessor.Current;
    
    using var scope = _logger.BeginScope(new Dictionary<string, object>
    {
        ["RequestId"] = context.TraceIdentifier,
        ["UserId"] = userContext?.UserId ?? "Anonymous",
        ["ClubId"] = userContext?.ClubId ?? "None",
        ["CorrelationId"] = userContext?.CorrelationId ?? Guid.NewGuid().ToString()
    });
    
    _logger.LogInformation("Processing request {Method} {Path}", 
        context.Request.Method, context.Request.Path);
        
    await next();
    
    _logger.LogInformation("Completed request with status {StatusCode}", 
        context.Response.StatusCode);
});
```

### Metrics Collection

```csharp
public class ApolloGatewayMetrics
{
    private readonly Counter<int> _requestCounter;
    private readonly Histogram<double> _requestDuration;
    
    public ApolloGatewayMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("Apollo.Gateway");
        _requestCounter = meter.CreateCounter<int>("apollo_gateway_requests_total");
        _requestDuration = meter.CreateHistogram<double>("apollo_gateway_request_duration_seconds");
    }
    
    public void RecordRequest(string method, string path, int statusCode, double duration)
    {
        _requestCounter.Add(1, new TagList
        {
            ["method"] = method,
            ["path"] = path,
            ["status_code"] = statusCode.ToString()
        });
        
        _requestDuration.Record(duration, new TagList
        {
            ["method"] = method,
            ["path"] = path
        });
    }
}
```

## 🔒 Security Best Practices

### JWT Security

1. **Token Validation**: Always validate tokens with AuthService
2. **Short Expiry**: Use short-lived access tokens (1 hour)
3. **Refresh Tokens**: Implement secure refresh token flow
4. **Secure Storage**: Never log or cache JWT tokens
5. **HTTPS Only**: Always use HTTPS in production

### Multi-Tenant Security

1. **Club Isolation**: Validate club access on every request
2. **Role Validation**: Check club-specific roles and permissions
3. **Data Filtering**: Never expose cross-club data
4. **Audit Logging**: Log all club context changes
5. **Session Management**: Implement proper session timeout

### Rate Limiting

1. **User-Based**: Limit requests per user
2. **Club-Based**: Limit requests per club
3. **Endpoint-Based**: Different limits for different operations
4. **IP-Based**: Additional protection against abuse
5. **Graceful Degradation**: Proper error responses when limited

## 📚 Additional Resources

- [Apollo User Context Guide](USER-CONTEXT-GUIDE.md)
- [Apollo Authentication Service](src/AuthService/README.md)
- [Apollo Club Management](src/ClubService/README.md)
- [Apollo Member Management](src/MemberService/README.md)

---

**Apollo** - Empowering sports clubs with secure, scalable technology 🚀

For support: [support@apollo-sports.com](mailto:support@apollo-sports.com) 
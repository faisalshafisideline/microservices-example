# API Gateway Implementation Guide

This guide provides detailed information about the YARP-based API Gateway with hardcoded authentication implemented in the microservices architecture.

## 🎯 Overview

The API Gateway serves as the single entry point for all client requests, providing:
- **Centralized Authentication**: Basic auth with hardcoded users
- **Authorization Policies**: Role-based access control
- **Request Routing**: YARP reverse proxy to backend services
- **Observability**: Request logging and health monitoring
- **Cross-cutting Concerns**: CORS, security headers, etc.

## 🏗️ Architecture Components

```
┌─────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Clients   │───▶│   API Gateway    │───▶│ Backend Services│
│             │    │                  │    │                 │
│ • Web Apps  │    │ • Authentication │    │ • Article       │
│ • Mobile    │    │ • Authorization  │    │ • Reporting     │
│ • APIs      │    │ • Routing (YARP) │    │                 │
└─────────────┘    │ • Logging        │    └─────────────────┘
                   │ • Health Checks  │
                   └──────────────────┘
```

## 🔐 Authentication Implementation

### Hardcoded User Store

Located in `src/ApiGateway/Authentication/IUserStore.cs`:

```csharp
public sealed class HardcodedUserStore : IUserStore
{
    private readonly List<User> _users = [
        new() {
            Username = "admin",
            Password = "supersecret", 
            Roles = ["Admin", "Reporter", "User"]
        },
        new() {
            Username = "reporter", 
            Password = "report123",
            Roles = ["Reporter", "User"]
        },
        // ... more users
    ];
}
```

### Custom Authentication Handler

Located in `src/ApiGateway/Authentication/HardcodedAuthenticationHandler.cs`:

Key features:
- **Basic Authentication**: Decodes `Authorization: Basic <base64>` headers
- **Claims Creation**: Converts user info to claims for authorization
- **Logging**: Comprehensive authentication logging
- **Error Handling**: Proper 401/403 responses

```csharp
protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
{
    // Extract and validate Basic auth header
    // Validate credentials against user store
    // Create claims identity with roles
    // Return authentication ticket
}
```

## 🛡️ Authorization Policies

### Policy Definitions

Located in `src/ApiGateway/Authorization/Policies.cs`:

```csharp
public static class Policies
{
    public const string AdminOnly = "AdminOnly";
    public const string ReporterOrAdmin = "ReporterOrAdmin"; 
    public const string AuthorOrAdmin = "AuthorOrAdmin";
    public const string AuthenticatedUser = "AuthenticatedUser";
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
        
        // Article endpoints
        _ when path.StartsWith("/api/articles") && method == "GET" => Policies.PublicRead,
        _ when path.StartsWith("/api/articles") && method == "POST" => Policies.AuthorOrAdmin,
        
        // Reporting endpoints  
        _ when path.StartsWith("/api/reporting") => Policies.ReporterOrAdmin,
        
        // Default
        _ => Policies.AuthenticatedUser
    };
}
```

## 🔄 YARP Configuration

### Route Configuration

Located in `src/ApiGateway/appsettings.json`:

```json
{
  "ReverseProxy": {
    "Routes": {
      "article-service-route": {
        "ClusterId": "article-service-cluster",
        "Match": {
          "Path": "/api/articles/{**catch-all}"
        },
        "Transforms": [
          {
            "PathPattern": "/api/articles/{**catch-all}"
          },
          {
            "RequestHeader": "X-Forwarded-For",
            "Append": "{RemoteIpAddress}"
          },
          {
            "RequestHeader": "X-Gateway-User", 
            "Append": "{user.identity.name}"
          }
        ]
      }
    },
    "Clusters": {
      "article-service-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://article-service:8080/"
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

- **Path Matching**: Route requests based on URL patterns
- **Header Transformation**: Add user context to downstream requests
- **Health Checking**: Monitor backend service health
- **Load Balancing**: Round-robin between multiple instances

## 📊 Gateway Endpoints

### Management Endpoints

Located in `src/ApiGateway/Endpoints/GatewayEndpoints.cs`:

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/gateway` | GET | None | Gateway information |
| `/api/gateway/user` | GET | Required | Current user info |
| `/api/gateway/health` | GET | None | Gateway health |
| `/api/gateway/auth/test` | POST | Required | Test authentication |
| `/api/gateway/routes` | GET | Admin | Available routes |

### Example Responses

**Gateway Info** (`/api/gateway`):
```json
{
  "name": "Microservices API Gateway",
  "version": "1.0.0",
  "environment": "Development",
  "authentication": {
    "scheme": "Basic",
    "supportedRoles": ["Admin", "Reporter", "Author", "User"]
  },
  "services": {
    "articleService": "/api/articles",
    "reportingService": "/api/reporting"
  }
}
```

**Current User** (`/api/gateway/user`):
```json
{
  "id": "admin-001",
  "username": "admin", 
  "email": "admin@example.com",
  "fullName": "System Administrator",
  "roles": ["Admin", "Reporter", "User"],
  "isAuthenticated": true
}
```

## 🧪 Testing Guide

### Basic Authentication Testing

```bash
# Test with different user credentials
curl -u admin:supersecret http://localhost:5000/api/gateway/user
curl -u reporter:report123 http://localhost:5000/api/gateway/user
curl -u author:write123 http://localhost:5000/api/gateway/user
curl -u user:user123 http://localhost:5000/api/gateway/user

# Test invalid credentials (should return 401)
curl -u invalid:credentials http://localhost:5000/api/gateway/user

# Test no credentials (should return 401)
curl http://localhost:5000/api/gateway/user
```

### Authorization Testing

```bash
# Admin access (should work)
curl -u admin:supersecret http://localhost:5000/api/gateway/routes

# Reporter access to admin endpoint (should return 403)
curl -u reporter:report123 http://localhost:5000/api/gateway/routes

# Author creating article (should work)
curl -X POST -u author:write123 \
  -H "Content-Type: application/json" \
  -d '{"title":"Test","content":"Test content","authorId":"1","authorName":"Test"}' \
  http://localhost:5000/api/articles

# User creating article (should return 403)
curl -X POST -u user:user123 \
  -H "Content-Type: application/json" \
  -d '{"title":"Test","content":"Test content","authorId":"1","authorName":"Test"}' \
  http://localhost:5000/api/articles
```

### Request Flow Testing

```bash
# Test request enrichment
curl -v -u admin:supersecret http://localhost:5000/api/articles

# Check response headers for:
# - X-Forwarded-For
# - Correlation IDs
# - Security headers
```

## 📝 Request/Response Flow

### 1. Authentication Flow

```
1. Client sends request with Authorization header
2. Gateway extracts Basic auth credentials
3. HardcodedAuthenticationHandler validates credentials
4. User store returns user info with roles
5. Authentication handler creates ClaimsIdentity
6. Request proceeds with authenticated user context
```

### 2. Authorization Flow

```
1. RouteBasedAuthorizationMiddleware examines request path/method
2. Determines required policy based on route rules
3. IAuthorizationService evaluates user claims against policy
4. If authorized, request continues to YARP proxy
5. If not authorized, returns 401/403 response
```

### 3. Proxying Flow

```
1. YARP matches request to configured route
2. Applies request transformations (headers, path)
3. Forwards request to backend service
4. Returns backend response to client
5. Logs request/response for observability
```

## 🔧 Configuration Options

### Authentication Settings

```json
{
  "Authentication": {
    "DefaultScheme": "HardcodedBasic",
    "EnableBasicAuth": true,
    "EnableJwtAuth": false
  }
}
```

### CORS Settings

```json
{
  "Cors": {
    "EnableCors": true,
    "PolicyName": "ApiGatewayPolicy",
    "AllowedOrigins": ["http://localhost:3000"],
    "AllowedMethods": ["GET", "POST", "PUT", "DELETE"],
    "AllowCredentials": true
  }
}
```

### Logging Settings

```json
{
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      {"Name": "Console"},
      {"Name": "File", "Args": {"path": "logs/gateway-.log"}}
    ]
  }
}
```

## 🚀 Production Considerations

### Security Enhancements

1. **Replace Hardcoded Users**:
   ```csharp
   // Instead of hardcoded users, use:
   services.AddAuthentication()
       .AddJwtBearer(options => {
           // JWT configuration
       });
   ```

2. **Use HTTPS**:
   ```json
   {
     "Kestrel": {
       "Endpoints": {
         "Https": {
           "Url": "https://+:443",
           "Certificate": {
             "Path": "certificate.pfx",
             "Password": "password"
           }
         }
       }
     }
   }
   ```

3. **Add Rate Limiting**:
   ```csharp
   services.AddRateLimiter(options => {
       options.AddFixedWindowLimiter("api", config => {
           config.Window = TimeSpan.FromMinutes(1);
           config.PermitLimit = 100;
       });
   });
   ```

### Performance Optimizations

1. **Connection Pooling**:
   ```json
   {
     "ReverseProxy": {
       "Clusters": {
         "article-service": {
           "HttpRequest": {
             "Timeout": "00:01:00"
           },
           "HttpClient": {
             "MaxConnectionsPerServer": 10
           }
         }
       }
     }
   }
   ```

2. **Response Caching**:
   ```csharp
   services.AddResponseCaching();
   app.UseResponseCaching();
   ```

### Monitoring & Observability

1. **Structured Logging**:
   ```csharp
   app.UseSerilogRequestLogging(options => {
       options.EnrichDiagnosticContext = (context, httpContext) => {
           context.Set("UserId", httpContext.User.FindFirst("sub")?.Value);
           context.Set("UserRoles", string.Join(",", httpContext.User.FindAll("role")));
       };
   });
   ```

2. **Health Checks**:
   ```csharp
   services.AddHealthChecks()
       .AddCheck<GatewayHealthCheck>("gateway")
       .AddUrlGroup(new Uri("http://article-service/health"), "article-service");
   ```

3. **Metrics Collection**:
   ```csharp
   services.AddOpenTelemetry()
       .WithMetrics(builder => builder
           .AddAspNetCoreInstrumentation()
           .AddRuntimeInstrumentation());
   ```

## 🛠️ Troubleshooting

### Common Issues

1. **401 Unauthorized**:
   - Check Authorization header format
   - Verify username/password combination
   - Review authentication handler logs

2. **403 Forbidden**:
   - Check user roles in JWT/claims
   - Verify authorization policy requirements
   - Review route-based authorization rules

3. **Service Unavailable**:
   - Check backend service health
   - Verify YARP cluster configuration
   - Review Docker network connectivity

### Debug Commands

```bash
# Check gateway logs
docker logs api-gateway

# Test backend service directly
curl http://localhost:5001/health  # If running locally
docker exec -it article-service curl http://localhost:8080/health

# Verify user claims
curl -u admin:supersecret http://localhost:5000/api/gateway/user | jq
```

### Log Analysis

Look for these log patterns:
- `Authentication successful for user: {username}`
- `Authorization failed for {path}. User: {user}, Policy: {policy}`
- `YARP: Proxying request to {destination}`
- `Health check failed for {service}`

## 📚 Further Reading

- [YARP Documentation](https://microsoft.github.io/reverse-proxy/)
- [ASP.NET Core Authentication](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/)
- [ASP.NET Core Authorization](https://docs.microsoft.com/en-us/aspnet/core/security/authorization/)
- [API Gateway Pattern](https://microservices.io/patterns/apigateway.html)
- [Security Best Practices](https://docs.microsoft.com/en-us/aspnet/core/security/) 
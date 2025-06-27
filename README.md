# Microservices Example - .NET 8 with API Gateway

A comprehensive example of a microservices architecture built with .NET 8, featuring clean architecture patterns, event-driven communication, gRPC inter-service communication, and a secure API Gateway with authentication.

## 🏗️ Architecture Overview

This solution demonstrates a microservices architecture with **three main services**:

### 1. API Gateway (NEW!)
- **Purpose**: Single entry point with authentication and authorization
- **Technology**: .NET 8 + YARP (Yet Another Reverse Proxy)
- **Features**:
  - Route-based request routing to backend services
  - Basic Authentication with hardcoded users
  - Role-based authorization policies
  - Request/response logging and monitoring
  - Health checking of downstream services
  - CORS support for web clients

### 2. Article Service
- **Purpose**: Manages article creation and retrieval
- **Technology**: .NET 8 Web API + gRPC Server
- **Database**: SQL Server (ArticleDb)
- **Responsibilities**:
  - Create and manage articles
  - Expose REST API endpoints (via Gateway)
  - Provide gRPC service for inter-service communication
  - Publish `ArticleCreatedEvent` via RabbitMQ

### 3. Reporting Service
- **Purpose**: Analytics and reporting for articles
- **Technology**: .NET 8 Web API + gRPC Client
- **Database**: SQL Server (ReportingDb)
- **Responsibilities**:
  - Track article views and analytics
  - Consume `ArticleCreatedEvent` from RabbitMQ
  - Call Article Service via gRPC for enriched data
  - Provide reporting endpoints (via Gateway)

## 🛠️ Technologies Used

- **.NET 8**: Modern C# web applications
- **YARP**: High-performance reverse proxy for .NET
- **Basic Authentication**: Hardcoded users for development/testing
- **Entity Framework Core**: Database ORM
- **gRPC**: High-performance inter-service communication
- **RabbitMQ + MassTransit**: Event-driven messaging
- **SQL Server**: Primary data storage
- **Docker**: Containerization
- **Serilog**: Structured logging
- **Carter**: Minimal API endpoints
- **FluentValidation**: Input validation
- **Swagger/OpenAPI**: API documentation

## 🔐 Authentication & Authorization

### Hardcoded Users (Development Only)

| Username | Password | Roles | Access Level |
|----------|----------|-------|--------------|
| `admin` | `supersecret` | Admin, Reporter, User | Full access |
| `reporter` | `report123` | Reporter, User | Reporting + basic access |
| `author` | `write123` | Author, User | Article creation + basic access |
| `user` | `user123` | User | Basic read access |

### Route-Based Authorization

| Route | Method | Required Role | Description |
|-------|--------|---------------|-------------|
| `/api/articles` | GET | Public | Anyone can read articles |
| `/api/articles` | POST | Author or Admin | Create articles |
| `/api/articles/{id}` | PUT/PATCH | Author or Admin | Update articles |
| `/api/articles/{id}` | DELETE | Admin | Delete articles |
| `/api/reporting/*` | GET | Reporter or Admin | Access reporting data |
| `/api/reporting/*` | POST | Reporter or Admin | Create reports |
| `/api/gateway/*` | GET | Varies | Gateway management |

## 📋 Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) or Docker SQL Server container

## 🚀 Quick Start

### 1. Clone the Repository
```bash
git clone <repository-url>
cd microservices-example
```

### 2. Start the Complete System
```bash
docker-compose up --build
```

This will start:
- **API Gateway**: http://localhost:5000 (Main entry point)
- **RabbitMQ Management**: http://localhost:15672 (admin/admin123)
- **SQL Server** instances for both services
- **Backend services** (internal only, accessible via Gateway)

### 3. Access the System

- **🌐 Main Gateway**: http://localhost:5000/swagger
- **📊 Gateway Info**: http://localhost:5000/api/gateway
- **🏥 Health Check**: http://localhost:5000/health
- **🐰 RabbitMQ**: http://localhost:15672

## 📡 API Endpoints (via Gateway)

### Gateway Management
- `GET /api/gateway` - Gateway information (public)
- `GET /api/gateway/user` - Current user info (authenticated)
- `GET /api/gateway/routes` - Available routes (admin only)
- `POST /api/gateway/auth/test` - Test authentication (authenticated)

### Article Service (via Gateway)
- `GET /api/articles` - Get articles (public)
- `POST /api/articles` - Create article (author/admin)
- `GET /api/articles/{id}` - Get specific article (public)
- `PUT /api/articles/{id}` - Update article (author/admin)
- `DELETE /api/articles/{id}` - Delete article (admin only)

### Reporting Service (via Gateway)
- `GET /api/reporting/articles/{id}/views` - Get view stats (reporter/admin)
- `POST /api/reporting/articles/{id}/views` - Record view (reporter/admin)
- `GET /api/reporting/articles/top-viewed` - Top articles (reporter/admin)
- `GET /api/reporting/authors/{id}/stats` - Author stats (reporter/admin)

## 📝 Usage Examples

### 1. Test Authentication
```bash
# Get gateway info (no auth required)
curl http://localhost:5000/api/gateway

# Get current user info (requires auth)
curl -u admin:supersecret http://localhost:5000/api/gateway/user
```

### 2. Create an Article (Author Role Required)
```bash
curl -X POST "http://localhost:5000/api/articles" \
  -u author:write123 \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Microservices with .NET 8",
    "content": "This article explains how to build microservices...",
    "authorId": "author-001",
    "authorName": "John Developer",
    "category": "Technology",
    "tags": ["microservices", "dotnet", "architecture"],
    "publishImmediately": true
  }'
```

### 3. Get Articles (Public Access)
```bash
# No authentication required for reading articles
curl http://localhost:5000/api/articles
```

### 4. Access Reporting Data (Reporter Role Required)
```bash
# Get article view statistics
curl -u reporter:report123 \
  "http://localhost:5000/api/reporting/articles/{articleId}/views"

# Get top viewed articles
curl -u reporter:report123 \
  "http://localhost:5000/api/reporting/articles/top-viewed?limit=5"
```

### 5. Admin Operations (Admin Role Required)
```bash
# Delete an article (admin only)
curl -X DELETE -u admin:supersecret \
  "http://localhost:5000/api/articles/{articleId}"

# Get all available routes
curl -u admin:supersecret \
  "http://localhost:5000/api/gateway/routes"
```

## 🔧 Configuration

### Environment Variables (Docker)
```bash
# API Gateway
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:8080

# Service Discovery (automatic via Docker networking)
# article-service:8080 (HTTP)
# article-service:8081 (gRPC)
# reporting-service:8080 (HTTP)
```

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

### YARP Proxy Configuration
```json
{
  "ReverseProxy": {
    "Routes": {
      "article-service-route": {
        "ClusterId": "article-service-cluster",
        "Match": { "Path": "/api/articles/{**catch-all}" }
      }
    },
    "Clusters": {
      "article-service-cluster": {
        "Destinations": {
          "destination1": { "Address": "http://article-service:8080/" }
        }
      }
    }
  }
}
```

## 🧪 Testing Authentication & Authorization

### Using curl with Basic Auth
```bash
# Test different user roles
curl -u admin:supersecret http://localhost:5000/api/gateway/user
curl -u reporter:report123 http://localhost:5000/api/gateway/user
curl -u author:write123 http://localhost:5000/api/gateway/user
curl -u user:user123 http://localhost:5000/api/gateway/user

# Test unauthorized access (should return 401)
curl http://localhost:5000/api/gateway/user

# Test forbidden access (should return 403)
curl -u user:user123 http://localhost:5000/api/articles -X POST
```

### Using Swagger UI
1. Navigate to http://localhost:5000/swagger
2. Click **"Authorize"** button
3. Enter credentials (e.g., username: `admin`, password: `supersecret`)
4. Test different endpoints with various user roles

## 🏛️ Architecture Patterns

### API Gateway Pattern
- **Single Entry Point**: All client requests go through the gateway
- **Cross-Cutting Concerns**: Authentication, logging, rate limiting
- **Service Discovery**: Routes requests to appropriate backend services
- **Protocol Translation**: HTTP to gRPC where needed

### Security Patterns
- **Authentication at Gateway**: Centralized authentication logic
- **Authorization Policies**: Role-based access control
- **Request Enrichment**: Add user context to downstream requests
- **Security Headers**: Automatic security header injection

### Observability Patterns
- **Centralized Logging**: All requests logged at gateway level
- **Request Tracing**: Correlation IDs for distributed tracing
- **Health Monitoring**: Gateway monitors downstream service health
- **Metrics Collection**: Performance and usage metrics

## 🔒 Security Considerations

### Current Implementation (Development)
- **Basic Authentication**: Simple username/password
- **Hardcoded Users**: In-memory user store
- **Plaintext Passwords**: ⚠️ NOT suitable for production
- **Role-Based Authorization**: Policy-based access control

### Production Recommendations
- **JWT Authentication**: Token-based authentication
- **OAuth2/OpenID Connect**: Industry standard protocols
- **External Identity Provider**: Azure AD, Auth0, etc.
- **Password Hashing**: bcrypt, Argon2, etc.
- **HTTPS Everywhere**: TLS encryption
- **Rate Limiting**: Prevent abuse
- **API Keys**: Service-to-service authentication
- **Secret Management**: Azure Key Vault, HashiCorp Vault

## 🚀 Deployment

### Local Development
```bash
# Start infrastructure only
docker-compose up -d rabbitmq article-db reporting-db

# Run services locally for debugging
cd src/ApiGateway && dotnet run
cd src/ArticleService && dotnet run  
cd src/ReportingService && dotnet run
```

### Production Docker
```bash
# Build and run complete system
docker-compose up --build

# Scale services (if needed)
docker-compose up --scale article-service=2 --scale reporting-service=2
```

## 📊 Monitoring & Observability

### Gateway Endpoints
- **Health**: http://localhost:5000/health
- **Gateway Health**: http://localhost:5000/api/gateway/health
- **Service Health**: 
  - http://localhost:5000/article-service/health
  - http://localhost:5000/reporting-service/health

### Logging
```bash
# View gateway logs
docker logs api-gateway

# View service logs  
docker logs article-service
docker logs reporting-service

# Follow all logs
docker-compose logs -f
```

### Request Flow Tracing
The gateway automatically adds headers to track requests:
- `X-Forwarded-For`: Client IP address
- `X-Gateway-User`: Authenticated username
- Correlation IDs for distributed tracing

## 🧩 Extension Points

### Adding JWT Authentication
1. Update `Program.cs` to add JWT authentication scheme
2. Create JWT token endpoint in gateway
3. Configure JWT validation middleware
4. Update authorization policies

### Adding Rate Limiting
1. Install `AspNetCoreRateLimit` package
2. Configure rate limiting policies in `appsettings.json`
3. Add rate limiting middleware to gateway pipeline

### Adding API Versioning
1. Configure YARP routes with version prefixes
2. Route different API versions to different service instances
3. Implement backward compatibility strategies

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Add comprehensive tests for authentication/authorization
4. Update documentation
5. Submit a pull request

## 📜 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 📞 Support

For questions or issues:
- Review the authentication examples above
- Check Docker container logs
- Test with different user roles using the provided credentials
- Create an issue for bugs or feature requests 
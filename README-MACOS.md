# 🍎 Running .NET 8 Microservices on macOS

This guide explains how to set up and run the complete microservices solution on macOS.

## 🔧 Prerequisites

### 1. Install .NET 8 SDK
```bash
# Option 1: Download from Microsoft
# Visit: https://dotnet.microsoft.com/download/dotnet/8.0

# Option 2: Using Homebrew (recommended)
brew install --cask dotnet-sdk

# Verify installation
dotnet --version  # Should show 8.x.x
```

### 2. Install Docker Desktop
```bash
# Option 1: Download from Docker
# Visit: https://www.docker.com/products/docker-desktop

# Option 2: Using Homebrew
brew install --cask docker

# Start Docker Desktop application and verify
docker --version
docker-compose --version
```

### 3. Install Git (if not already installed)
```bash
# Check if installed
git --version

# Install if needed
brew install git
```

## 🚀 Quick Start

### Option 1: Automated Setup (Recommended)
```bash
# Clone the repository
git clone <your-repo-url>
cd microservices-example

# Run the automated setup script
./run-local.sh
```

This script will:
- ✅ Check if Docker is running
- ✅ Verify required ports are available
- ✅ Start SQL Server, RabbitMQ, and Redis containers
- ✅ Build the .NET solution
- ✅ Launch all microservices in separate terminal windows

### Option 2: Manual Setup

#### Step 1: Start Infrastructure Services
```bash
# Start SQL Server
docker run -d --name sqlserver \
    -e "ACCEPT_EULA=Y" \
    -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd" \
    -p 1433:1433 \
    mcr.microsoft.com/mssql/server:2022-latest

# Start RabbitMQ with Management UI
docker run -d --name rabbitmq \
    -e RABBITMQ_DEFAULT_USER=admin \
    -e RABBITMQ_DEFAULT_PASS=admin \
    -p 5672:5672 \
    -p 15672:15672 \
    rabbitmq:3-management

# Start Redis
docker run -d --name redis \
    -p 6379:6379 \
    redis:alpine
```

#### Step 2: Build the Solution
```bash
dotnet build
```

#### Step 3: Start the Services
Open 3 separate terminal windows and run:

**Terminal 1 - Article Service:**
```bash
dotnet run --project src/ArticleService/ArticleService.csproj --urls http://localhost:8081
```

**Terminal 2 - Reporting Service:**
```bash
dotnet run --project src/ReportingService/ReportingService.csproj --urls http://localhost:8082
```

**Terminal 3 - API Gateway:**
```bash
dotnet run --project src/ApiGateway/ApiGateway.csproj --urls http://localhost:8080
```

## 🌐 Service URLs

Once everything is running, you can access:

| Service | URL | Description |
|---------|-----|-------------|
| **API Gateway** | http://localhost:8080 | Main entry point |
| **API Gateway Swagger** | http://localhost:8080/swagger | API documentation |
| **Article Service** | http://localhost:8081 | Direct article service access |
| **Reporting Service** | http://localhost:8082 | Direct reporting service access |
| **RabbitMQ Management** | http://localhost:15672 | Message queue admin (admin/admin) |

### Database Connections
- **SQL Server**: `localhost,1433` (sa/YourStrong@Passw0rd)
- **Redis**: `localhost:6379`

## 🧪 Testing the Solution

### 1. Test API Gateway Health
```bash
curl http://localhost:8080/api/gateway/health
```

### 2. Create an Article
```bash
curl -X POST http://localhost:8080/api/articles \
  -H "Content-Type: application/json" \
  -H "Authorization: Basic YWRtaW46YWRtaW4=" \
  -d '{
    "title": "Test Article",
    "content": "This is a test article content.",
    "authorId": "author-123",
    "authorName": "John Doe",
    "category": "Technology",
    "tags": ["test", "demo"],
    "summary": "A test article for demonstration"
  }'
```

### 3. Get Articles
```bash
curl http://localhost:8080/api/articles
```

### 4. View Article Reports
```bash
curl http://localhost:8080/api/reporting/top-viewed \
  -H "Authorization: Basic cmVwb3J0ZXI6cmVwb3J0ZXI="
```

## 🛑 Stopping the Solution

### Automated Stop
```bash
./stop-local.sh
```

### Manual Stop
```bash
# Stop Docker containers
docker stop sqlserver rabbitmq redis
docker rm sqlserver rabbitmq redis

# Stop .NET services (Ctrl+C in each terminal)
# Or kill processes
pkill -f "dotnet.*ArticleService"
pkill -f "dotnet.*ReportingService"
pkill -f "dotnet.*ApiGateway"
```

## 🔍 Troubleshooting

### Common Issues

#### 1. Port Already in Use
```bash
# Check what's using a port
lsof -i :8080

# Kill process using port
kill -9 <PID>
```

#### 2. Docker Not Running
```bash
# Start Docker Desktop application
open /Applications/Docker.app

# Or check if Docker daemon is running
docker info
```

#### 3. SQL Server Connection Issues
```bash
# Check if SQL Server container is running
docker ps | grep sqlserver

# Check container logs
docker logs sqlserver
```

#### 4. Build Errors
```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

#### 5. Database Creation Issues
The services automatically create databases on startup. If you encounter issues:
```bash
# Restart SQL Server container
docker restart sqlserver

# Wait 30 seconds, then restart the services
```

### Authentication

The solution uses basic authentication with these test users:

| Username | Password | Roles |
|----------|----------|-------|
| admin | admin | Admin |
| author | author | Author |
| reporter | reporter | Reporter |
| user | user | User |

## 🏗️ Architecture Overview

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   API Gateway   │    │ Article Service │    │Reporting Service│
│   (Port 8080)   │◄──►│   (Port 8081)   │◄──►│   (Port 8082)   │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         └───────────────────────┼───────────────────────┘
                                 │
         ┌─────────────────┬─────┴─────┬─────────────────┐
         │                 │           │                 │
    ┌────▼────┐       ┌────▼────┐ ┌────▼────┐       ┌────▼────┐
    │SQL Server│       │RabbitMQ │ │  Redis  │       │ gRPC    │
    │Port 1433│       │Port 5672│ │Port 6379│       │Inter-svc│
    └─────────┘       └─────────┘ └─────────┘       └─────────┘
```

## 📝 Next Steps

1. **Enable Scalability Services**: Uncomment the scalability services in Program.cs files
2. **Configure Production Settings**: Update connection strings for production
3. **Add Monitoring**: Integrate with Application Insights or similar
4. **Security**: Implement proper JWT authentication
5. **Load Testing**: Use tools like k6 or Artillery for performance testing

## 🆘 Getting Help

If you encounter issues:
1. Check the terminal output for error messages
2. Verify all Docker containers are running: `docker ps`
3. Check service logs in the terminal windows
4. Ensure all required ports are available
5. Try restarting the infrastructure services

Happy coding! 🚀 
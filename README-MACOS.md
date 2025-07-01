# 🍎 Running Apollo Sports Club Management Platform on macOS

This guide explains how to set up and run the complete Apollo microservices solution on macOS.

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
- ✅ Verify required ports are available (8080-8084)
- ✅ Start SQL Server, RabbitMQ, and Redis containers
- ✅ Build the Apollo .NET solution
- ✅ Launch all Apollo microservices in separate terminal windows

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

#### Step 2: Build the Apollo Solution
```bash
dotnet build
```

#### Step 3: Start the Apollo Services
Open 5 separate terminal windows and run:

**Terminal 1 - Auth Service:**
```bash
dotnet run --project src/AuthService/AuthService.csproj --urls http://localhost:8081
```

**Terminal 2 - Club Service:**
```bash
dotnet run --project src/ClubService/ClubService.csproj --urls http://localhost:8082
```

**Terminal 3 - Member Service:**
```bash
dotnet run --project src/MemberService/MemberService.csproj --urls http://localhost:8083
```

**Terminal 4 - Communication Service:**
```bash
dotnet run --project src/CommunicationService/CommunicationService.csproj --urls http://localhost:8084
```

**Terminal 5 - API Gateway:**
```bash
dotnet run --project src/ApiGateway/ApiGateway.csproj --urls http://localhost:8080
```

## 🌐 Apollo Service URLs

Once everything is running, you can access:

| Service | URL | Description |
|---------|-----|-------------|
| **🌐 API Gateway** | http://localhost:8080 | Main entry point for Apollo |
| **🌐 API Gateway Swagger** | http://localhost:8080/swagger | Unified API documentation |
| **🔐 Auth Service** | http://localhost:8081 | Authentication & authorization |
| **🔐 Auth Service Swagger** | http://localhost:8081/swagger | Auth API documentation |
| **🏢 Club Service** | http://localhost:8082 | Club & tenant management |
| **🏢 Club Service Swagger** | http://localhost:8082/swagger | Club API documentation |
| **👥 Member Service** | http://localhost:8083 | Member profiles & management |
| **👥 Member Service Swagger** | http://localhost:8083/swagger | Member API documentation |
| **📧 Communication Service** | http://localhost:8084 | Notifications & messaging |
| **📧 Communication Service Swagger** | http://localhost:8084/swagger | Communication API documentation |
| **🐰 RabbitMQ Management** | http://localhost:15672 | Message queue admin (admin/admin) |

### Infrastructure Connections
- **SQL Server**: `localhost,1433` (sa/YourStrong@Passw0rd)
- **Redis**: `localhost:6379`

## 🧪 Testing Apollo Platform

### 1. Test API Gateway Health
```bash
curl http://localhost:8080/health
```

### 2. Register a New User
```bash
curl -X POST http://localhost:8081/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john.doe@example.com",
    "password": "SecurePass123!",
    "firstName": "John",
    "lastName": "Doe"
  }'
```

### 3. Login and Get JWT Token
```bash
curl -X POST http://localhost:8081/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@apollo-sports.com",
    "password": "admin123"
  }'
```

### 4. Create a Sports Club
```bash
curl -X POST http://localhost:8082/api/clubs \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <your-jwt-token>" \
  -d '{
    "name": "Barcelona Football Club",
    "code": "FC-BARCELONA",
    "country": "Spain",
    "primaryContactEmail": "admin@fcbarcelona.com",
    "primaryContactName": "Joan Laporta",
    "address": {
      "street": "Carrer de Aristides Maillol",
      "city": "Barcelona",
      "state": "Catalonia",
      "postalCode": "08028",
      "country": "Spain"
    }
  }'
```

### 5. Add a Club Member
```bash
curl -X POST http://localhost:8083/api/members \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <your-jwt-token>" \
  -d '{
    "clubId": "<club-id>",
    "firstName": "Lionel",
    "lastName": "Messi",
    "email": "messi@fcbarcelona.com",
    "membershipType": "Premium",
    "sports": ["Football"],
    "position": "Forward"
  }'
```

### 6. Send Club Notification
```bash
curl -X POST http://localhost:8084/api/notifications \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <your-jwt-token>" \
  -d '{
    "clubId": "<club-id>",
    "recipientIds": ["<member-id>"],
    "type": "Email",
    "subject": "Welcome to the Club!",
    "content": "Welcome to our sports club family!"
  }'
```

## 🛑 Stopping Apollo Platform

### Automated Stop
```bash
./stop-local.sh
```

### Manual Stop
```bash
# Stop Docker containers
docker stop sqlserver rabbitmq redis
docker rm sqlserver rabbitmq redis

# Stop Apollo services (Ctrl+C in each terminal)
# Or kill processes
pkill -f "dotnet.*AuthService"
pkill -f "dotnet.*ClubService"
pkill -f "dotnet.*MemberService"
pkill -f "dotnet.*CommunicationService"
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
# Test SQL Server connection
docker exec -it sqlserver /opt/mssql-tools/bin/sqlcmd \
    -S localhost -U sa -P 'YourStrong@Passw0rd' \
    -Q "SELECT @@VERSION"
```

#### 4. RabbitMQ Connection Issues
```bash
# Check RabbitMQ status
curl -u admin:admin http://localhost:15672/api/overview
```

#### 5. JWT Token Issues
- Ensure you're using the correct admin credentials: `admin@apollo-sports.com` / `admin123`
- Check token expiration (default: 1 hour)
- Verify the `Authorization: Bearer <token>` header format

#### 6. Multi-Tenant Access Issues
- Ensure the user has proper club roles assigned
- Check that the club ID exists and is active
- Verify subscription plan allows the operation

## 🏗️ Apollo Architecture

### Microservices Overview
- **🔐 AuthService**: OAuth2 + JWT authentication with 2FA support
- **🏢 ClubService**: Multi-tenant club management with subscription tiers
- **👥 MemberService**: Comprehensive member profiles with sports features
- **📧 CommunicationService**: Email/SMS/push notifications
- **🌐 API Gateway**: Unified entry point with routing and authentication

### Key Features
- ✅ **Multi-Tenancy**: Complete club data separation
- ✅ **Sports-Specific**: Teams, positions, skill levels, medical records
- ✅ **Subscription Management**: Free to Enterprise tiers
- ✅ **Communication**: Multi-channel notifications
- ✅ **Security**: JWT tokens, 2FA, role-based access
- ✅ **Scalability**: Event-driven architecture with MassTransit

## 📚 Additional Resources

- [User Context Guide](USER-CONTEXT-GUIDE.md)
- [API Gateway Guide](API-GATEWAY-GUIDE.md)
- [Improvement Plan](IMPROVEMENT-PLAN.md)
- [Scalability Guide](SCALABILITY-ROBUSTNESS-IMPROVEMENTS.md)

---

**Apollo** - Empowering sports clubs with modern technology 🚀

For support: [support@apollo-sports.com](mailto:support@apollo-sports.com) 
#!/bin/bash

# Microservices Local Development Setup for macOS
echo "🚀 Starting Microservices Solution on macOS..."

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker is not running. Please start Docker Desktop first."
    exit 1
fi

# Function to check if a port is in use
check_port() {
    if lsof -Pi :$1 -sTCP:LISTEN -t >/dev/null ; then
        echo "⚠️  Port $1 is already in use. Please stop the service using this port."
        return 1
    fi
    return 0
}

# Check required ports
echo "🔍 Checking required ports..."
PORTS=(1433 5672 6379 8080 8081 8082 15672)
for port in "${PORTS[@]}"; do
    if ! check_port $port; then
        echo "Port $port is required. Please free it up and try again."
        exit 1
    fi
done

# Start infrastructure services
echo "🏗️  Starting infrastructure services..."

# Start SQL Server
echo "📊 Starting SQL Server..."
docker run -d --name sqlserver \
    -e "ACCEPT_EULA=Y" \
    -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd" \
    -p 1433:1433 \
    mcr.microsoft.com/mssql/server:2022-latest

# Start RabbitMQ
echo "🐰 Starting RabbitMQ..."
docker run -d --name rabbitmq \
    -e RABBITMQ_DEFAULT_USER=admin \
    -e RABBITMQ_DEFAULT_PASS=admin \
    -p 5672:5672 \
    -p 15672:15672 \
    rabbitmq:3-management

# Start Redis
echo "🔴 Starting Redis..."
docker run -d --name redis \
    -p 6379:6379 \
    redis:alpine

# Wait for services to be ready
echo "⏳ Waiting for services to start..."
sleep 10

# Build the solution
echo "🔨 Building the solution..."
dotnet build

if [ $? -ne 0 ]; then
    echo "❌ Build failed. Please check the errors above."
    exit 1
fi

# Create databases
echo "💾 Creating databases..."
sleep 5  # Give SQL Server more time to fully start

echo "✅ Infrastructure services started successfully!"
echo ""
echo "📋 Service URLs:"
echo "   🌐 API Gateway: http://localhost:8080"
echo "   📝 Article Service: http://localhost:8081"
echo "   📊 Reporting Service: http://localhost:8082"
echo "   🐰 RabbitMQ Management: http://localhost:15672 (admin/admin)"
echo "   📊 SQL Server: localhost,1433 (sa/YourStrong@Passw0rd)"
echo "   🔴 Redis: localhost:6379"
echo ""
echo "🚀 Starting .NET services..."

# Start services in separate terminal windows/tabs
if command -v osascript &> /dev/null; then
    # macOS with Terminal app
    osascript -e 'tell app "Terminal" to do script "cd '$(pwd)' && echo \"🚀 Starting Article Service...\" && dotnet run --project src/ArticleService/ArticleService.csproj --urls http://localhost:8081"'
    sleep 3
    osascript -e 'tell app "Terminal" to do script "cd '$(pwd)' && echo \"🚀 Starting Reporting Service...\" && dotnet run --project src/ReportingService/ReportingService.csproj --urls http://localhost:8082"'
    sleep 3
    osascript -e 'tell app "Terminal" to do script "cd '$(pwd)' && echo \"🚀 Starting API Gateway...\" && dotnet run --project src/ApiGateway/ApiGateway.csproj --urls http://localhost:8080"'
else
    echo "📝 Manual startup required:"
    echo "   Terminal 1: dotnet run --project src/ArticleService/ArticleService.csproj --urls http://localhost:8081"
    echo "   Terminal 2: dotnet run --project src/ReportingService/ReportingService.csproj --urls http://localhost:8082"
    echo "   Terminal 3: dotnet run --project src/ApiGateway/ApiGateway.csproj --urls http://localhost:8080"
fi

echo ""
echo "✅ Setup complete! Check the new terminal windows for service logs."
echo "🌐 Open http://localhost:8080/swagger to test the API Gateway" 
#!/bin/bash

# Stop Microservices Local Development Environment
echo "🛑 Stopping Microservices Solution..."

# Stop and remove Docker containers
echo "🐳 Stopping Docker containers..."
docker stop sqlserver rabbitmq redis 2>/dev/null
docker rm sqlserver rabbitmq redis 2>/dev/null

# Kill .NET processes
echo "🔄 Stopping .NET services..."
pkill -f "dotnet.*ArticleService"
pkill -f "dotnet.*ReportingService" 
pkill -f "dotnet.*ApiGateway"

echo "✅ All services stopped!"
echo "💡 To clean up Docker volumes, run: docker volume prune" 
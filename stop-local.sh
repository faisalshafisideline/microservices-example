#!/bin/bash

# Stop Apollo Sports Club Management Platform
echo "🛑 Stopping Apollo Platform..."

# Stop and remove Docker containers
echo "🐳 Stopping Docker containers..."
docker stop sqlserver rabbitmq redis 2>/dev/null
docker rm sqlserver rabbitmq redis 2>/dev/null

# Kill .NET processes
echo "🔄 Stopping Apollo services..."
pkill -f "dotnet.*AuthService"
pkill -f "dotnet.*ClubService"
pkill -f "dotnet.*MemberService"
pkill -f "dotnet.*CommunicationService"
pkill -f "dotnet.*ApiGateway"

echo "✅ Apollo Platform stopped!"
echo "💡 To clean up Docker volumes, run: docker volume prune" 
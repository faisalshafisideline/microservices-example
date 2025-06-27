FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files
COPY ["ApiGateway.csproj", "ApiGateway/"]

# Restore dependencies
RUN dotnet restore "ApiGateway/ApiGateway.csproj"

# Copy source code
COPY . ApiGateway/

WORKDIR "/src/ApiGateway"
RUN dotnet build "ApiGateway.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ApiGateway.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create logs directory
RUN mkdir -p /app/logs

ENTRYPOINT ["dotnet", "ApiGateway.dll"] 
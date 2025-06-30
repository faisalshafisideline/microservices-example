# 🚀 Apollo - Multi-Tenant Sports Club Management Platform

A comprehensive .NET 8 microservices architecture for managing sports clubs, members, and operations at scale.

## 📊 **Scale & Impact**
- **472,432+ users** across multiple clubs and federations
- **Multi-tenant architecture** with club-level data isolation
- **Multi-language support** with centralized translation management
- **Multi-currency billing** with region-specific VAT handling

## 🏗️ **Architecture Overview**

### Core Microservices
- **🔐 AuthService** - OAuth2 + JWT + Multi-tenant + 2FA
- **🏢 ClubService** - Club/tenant management & configuration  
- **👥 MemberService** - Member profiles & management
- **📧 CommunicationService** - Email/SMS/Push notifications
- **💰 PaymentService** - Billing, invoicing & multi-currency
- **🌍 LocalizationService** - Multi-language & timezone handling

### Infrastructure Services
- **🚪 API Gateway** - YARP reverse proxy with authentication
- **🔍 Shared Contracts** - gRPC, events, user context propagation

## 🚀 **Key Features**

### Multi-Tenancy
- **Club-level data isolation** with configurable database partitioning
- **Per-club configurations**: language, currency, timezone, VAT rates
- **Role-based access control** per club and per service

### Scalability & Performance
- **gRPC inter-service communication** for high performance
- **Event-driven architecture** with RabbitMQ + MassTransit
- **CQRS pattern** with read/write separation
- **Distributed caching** with Redis
- **Circuit breakers & retry policies** for resilience

### Security & Compliance
- **OAuth2 + JWT** authentication with custom server
- **GDPR compliance** with data retention policies
- **Encryption** at rest and in transit
- **Multi-factor authentication** (TOTP + backup codes)
- **Azure Key Vault** for secrets management

### Internationalization
- **Weblate integration** for centralized translation management
- **NodaTime** for proper timezone handling
- **Multi-currency support** with conversion capabilities
- **Localized email templates** and notifications

## 🛠️ **Technology Stack**

### Backend
- **.NET 8** - Latest LTS with native AOT support
- **Entity Framework Core** - Database ORM with migrations
- **MediatR** - CQRS and mediator pattern
- **Carter** - Minimal API framework
- **gRPC** - High-performance inter-service communication
- **MassTransit + RabbitMQ** - Event-driven messaging
- **Serilog** - Structured logging with OpenTelemetry

### Infrastructure
- **YARP** - Reverse proxy and API gateway
- **Docker + Docker Compose** - Containerization
- **Azure SQL Server** - Primary database
- **Redis** - Distributed caching
- **Azure Blob Storage** - File storage
- **Azure Key Vault** - Secrets management

### Observability
- **OpenTelemetry** - Distributed tracing
- **Sentry** - Error monitoring
- **Azure Application Insights** - APM and metrics
- **Health checks** - Service monitoring

## 🏃‍♂️ **Quick Start**

### Prerequisites
- .NET 8 SDK
- Docker Desktop
- SQL Server (or Docker container)

### Local Development
```bash
# Clone and setup
git clone <repository>
cd microservices-example

# Start infrastructure
./run-local.sh

# The services will be available at:
# - API Gateway: http://localhost:5000
# - AuthService: http://localhost:5001  
# - ClubService: http://localhost:5002
# - MemberService: http://localhost:5003
# - CommunicationService: http://localhost:5004
# - PaymentService: http://localhost:5005
# - LocalizationService: http://localhost:5006
```

### Authentication
The system uses OAuth2 with JWT tokens. Default test users:
- **admin@lisa.ai** / `admin123` - System administrator
- **manager@club1.lisa.ai** / `manager123` - Club manager
- **member@club1.lisa.ai** / `member123` - Club member

## 📡 **API Examples**

### Authentication
```bash
# Login and get JWT token
curl -X POST http://localhost:5000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username": "admin@lisa.ai", "password": "admin123"}'
```

### Club Management
```bash
# Get club information
curl -X GET http://localhost:5000/api/clubs/my-club \
  -H "Authorization: Bearer <your-jwt-token>"

# Create new member
curl -X POST http://localhost:5000/api/members \
  -H "Authorization: Bearer <your-jwt-token>" \
  -H "Content-Type: application/json" \
  -d '{"email": "new@member.com", "firstName": "John", "lastName": "Doe"}'
```

### Multi-Language Support
```bash
# Get translations for Dutch
curl -X GET http://localhost:5000/api/localization/translations/nl-NL \
  -H "Authorization: Bearer <your-jwt-token>"
```

## 🏢 **Multi-Tenant Architecture**

### Club Isolation Strategies
1. **Database per Club** - Complete isolation (high-security clubs)
2. **Schema per Club** - Shared database with separate schemas  
3. **Row-level Security** - Shared tables with tenant filtering

### Configuration Hierarchy
```
System Level
├── Default language, currency, timezone
├── Default VAT rates per country
└── System-wide features

Club Level  
├── Override language, currency, timezone
├── Custom VAT rates
├── Club-specific features
└── Member role definitions

User Level
└── Personal language preference
```

## 🔄 **Event-Driven Communication**

### Key Events
- **ClubCreated** - New club registration
- **MemberJoined** - New member signup
- **PaymentProcessed** - Billing events
- **NotificationSent** - Communication tracking
- **LocalizationUpdated** - Translation changes

### Event Flow Example
```
Member Registration Flow:
1. MemberService → MemberCreated event
2. CommunicationService → Send welcome email
3. PaymentService → Setup billing profile
4. LocalizationService → Apply club language settings
```

## 📊 **Monitoring & Observability**

### Health Checks
- Service health: `/health`
- Database connectivity: `/health/db`
- External dependencies: `/health/dependencies`

### Metrics
- Request/response times
- Error rates per service
- Database query performance
- Cache hit/miss ratios
- Queue processing rates

### Distributed Tracing
All requests are traced across services using OpenTelemetry with correlation IDs.

## 🚀 **Deployment**

### Production Deployment
```bash
# Build and deploy to Azure
docker-compose -f docker-compose.production.yml up -d

# Or use Azure Container Apps
az containerapp up --source .
```

### Environment Configuration
- **Development**: Local Docker containers
- **Staging**: Azure Container Apps with shared resources
- **Production**: Azure Container Apps with dedicated resources + multi-region

## 🤝 **Contributing**

### Development Workflow
1. **AI-Assisted Development** - Use GitHub Copilot/Claude for code generation
2. **Human Review** - All AI-generated code must be reviewed
3. **Testing** - Unit, integration, and E2E tests required
4. **Documentation** - Update Confluence and README files

### Code Standards
- **Clean Architecture** - Domain, Application, Infrastructure layers
- **SOLID Principles** - Maintainable and testable code
- **Functional Programming** - Immutable objects, pure functions
- **Short-Circuit Evaluation** - Early returns, guard clauses

## 📚 **Documentation**

- **Architecture Decisions**: See `/docs/adr/` folder
- **API Documentation**: Available at `/swagger` on each service
- **Confluence**: Internal team documentation
- **OpenAPI Specs**: Auto-generated from code

## 🔒 **Security Considerations**

### Production Security
- Replace hardcoded users with proper OAuth provider
- Enable rate limiting and DDoS protection
- Implement proper RBAC with fine-grained permissions
- Regular security audits and penetration testing
- Secrets rotation and key management

### GDPR Compliance
- Data retention policies with automated cleanup
- User data export and deletion endpoints
- Audit trails for all data access
- Privacy by design principles

---

**Apollo** - Empowering sports clubs with modern technology 🚀

For support: [support@apollo-sports.com](mailto:support@apollo-sports.com) 
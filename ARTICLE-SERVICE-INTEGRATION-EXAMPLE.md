# Apollo Services - User Context Integration Example

This example shows how to integrate the User Context system into Apollo's microservices with multi-tenant club management.

## 1. Update Apollo Service Project References

Add reference to Shared.Contracts in each Apollo service:

```xml
<ItemGroup>
  <ProjectReference Include="..\Shared\Contracts\Shared.Contracts.csproj" />
</ItemGroup>
```

## 2. Update Program.cs for Apollo Services

Example for MemberService:

```csharp
using MemberService.Application.Commands;
using MemberService.Application.Repositories;
using MemberService.Infrastructure.Data;
using MemberService.Infrastructure.Repositories;
using Carter;
using FluentValidation;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Reflection;
// Add these imports for Apollo
using Shared.Contracts.UserContext.Extensions;
using Shared.Contracts.UserContext.Interceptors;

var builder = WebApplication.CreateBuilder(args);

// ... existing Apollo configuration ...

// Add Apollo User Context services
builder.Services.AddUserContext();
builder.Services.AddUserContextGrpcInterceptors();

// Update gRPC configuration for Apollo
builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<UserContextServerInterceptor>();
});

// Add gRPC clients for other Apollo services
builder.Services.AddGrpcClient<AuthService.AuthServiceClient>(options =>
{
    options.Address = new Uri("http://localhost:8081");
})
.AddInterceptor<UserContextClientInterceptor>();

builder.Services.AddGrpcClient<ClubService.ClubServiceClient>(options =>
{
    options.Address = new Uri("http://localhost:8082");
})
.AddInterceptor<UserContextClientInterceptor>();

// Update MassTransit configuration for Apollo events
builder.Services.AddMassTransit(x =>
{
    x.ConfigureUserContextPropagation(); // Add Apollo context propagation
    
    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitMqHost = builder.Configuration.GetValue<string>("RabbitMQ:Host") ?? "localhost";
        var rabbitMqUsername = builder.Configuration.GetValue<string>("RabbitMQ:Username") ?? "admin";
        var rabbitMqPassword = builder.Configuration.GetValue<string>("RabbitMQ:Password") ?? "admin";

        cfg.Host(rabbitMqHost, h =>
        {
            h.Username(rabbitMqUsername);
            h.Password(rabbitMqPassword);
        });

        cfg.ConfigureUserContextPropagation(context); // Add Apollo context
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

// Add Apollo User Context middleware
app.UseUserContext(); // Add this after JWT authentication

// ... rest of Apollo configuration ...
```

## 3. Update Apollo Member Command Handler

```csharp
using MemberService.Application.Commands;
using MemberService.Domain.Entities;
using MemberService.Domain.ValueObjects;
using MediatR;
using MassTransit;
using Shared.Contracts.Messages;
using Shared.Contracts.UserContext; // Add this for Apollo

namespace MemberService.Application.Commands;

public class AddMemberCommandHandler : IRequestHandler<AddMemberCommand, Guid>
{
    private readonly IMemberRepository _memberRepository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IUserContextAccessor _userContextAccessor; // Add Apollo context
    private readonly AuthService.AuthServiceClient _authClient; // Apollo AuthService
    private readonly ILogger<AddMemberCommandHandler> _logger;

    public AddMemberCommandHandler(
        IMemberRepository memberRepository,
        IPublishEndpoint publishEndpoint,
        IUserContextAccessor userContextAccessor, // Add Apollo context
        AuthService.AuthServiceClient authClient, // Apollo AuthService
        ILogger<AddMemberCommandHandler> logger)
    {
        _memberRepository = memberRepository;
        _publishEndpoint = publishEndpoint;
        _userContextAccessor = userContextAccessor; // Add Apollo context
        _authClient = authClient; // Apollo AuthService
        _logger = logger;
    }

    public async Task<Guid> Handle(AddMemberCommand request, CancellationToken cancellationToken)
    {
        // Get Apollo user context with club information
        var userContext = _userContextAccessor.GetRequiredContext();
        
        _logger.LogInformation("Adding member to club {ClubId} by user: {UserId}, Correlation: {CorrelationId}",
            request.ClubId, userContext.UserId, userContext.CorrelationId);

        // Apollo: Validate club access via AuthService
        var clubAccess = await _authClient.ValidateClubAccessAsync(new ValidateClubAccessRequest
        {
            UserId = userContext.UserId,
            ClubId = request.ClubId.ToString(),
            RequiredPermission = "ManageMembers"
        });

        if (!clubAccess.HasAccess)
        {
            throw new UnauthorizedAccessException($"User {userContext.UserId} not authorized to manage members in club {request.ClubId}");
        }

        // Create Apollo member with sports-specific data
        var member = new Member(
            clubId: request.ClubId,
            userId: request.UserId ?? Guid.Parse(userContext.UserId),
            memberNumber: request.MemberNumber,
            firstName: request.FirstName,
            lastName: request.LastName,
            email: request.Email,
            membershipType: request.MembershipType,
            membershipFee: request.MembershipFee,
            currency: request.Currency,
            createdBy: userContext.UserId
        );

        // Add sports-specific information
        if (request.Sports?.Any() == true)
        {
            member.UpdateSportsInformation(request.Sports, request.Position, request.SkillLevel);
        }

        await _memberRepository.AddAsync(member, cancellationToken);

        // Publish Apollo MemberJoinedEvent - context will be automatically propagated
        await _publishEndpoint.Publish(new MemberJoinedEvent
        {
            ClubId = member.ClubId,
            MemberId = member.Id,
            MemberEmail = member.Email,
            MemberName = $"{member.FirstName} {member.LastName}",
            MembershipType = member.MembershipType.ToString(),
            JoinedAt = member.CreatedAt,
            Sports = member.Sports.ToList()
        }, cancellationToken);

        _logger.LogInformation("Member {MemberId} added to club {ClubId}, Correlation: {CorrelationId}",
            member.Id, member.ClubId, userContext.CorrelationId);

        return member.Id;
    }
}
```

## 4. Update Apollo Club gRPC Service

```csharp
using ClubService.Application.Queries;
using Grpc.Core;
using MediatR;
using Shared.Contracts.Protos;
using Shared.Contracts.UserContext; // Add this for Apollo

namespace ClubService.Services;

public class ClubGrpcService : Shared.Contracts.Protos.ClubService.ClubServiceBase
{
    private readonly IMediator _mediator;
    private readonly IUserContextAccessor _userContextAccessor; // Add Apollo context
    private readonly ILogger<ClubGrpcService> _logger;

    public ClubGrpcService(
        IMediator mediator, 
        IUserContextAccessor userContextAccessor, // Add Apollo context
        ILogger<ClubGrpcService> logger)
    {
        _mediator = mediator;
        _userContextAccessor = userContextAccessor; // Add Apollo context
        _logger = logger;
    }

    public override async Task<GetClubResponse> GetClub(GetClubRequest request, ServerCallContext context)
    {
        // Apollo: Context is automatically extracted by UserContextServerInterceptor
        var userContext = _userContextAccessor.Current;
        
        _logger.LogInformation("Processing gRPC GetClub request for user: {UserId}, Club: {ClubId}, Correlation: {CorrelationId}",
            userContext?.UserId, request.ClubId, userContext?.CorrelationId);

        try
        {
            // Apollo: Validate club access
            if (userContext?.ClubId != request.ClubId && !userContext?.Roles.Contains("SystemAdmin") == true)
            {
                throw new RpcException(new Status(StatusCode.PermissionDenied, "Access denied to club"));
            }

            var query = new GetClubQuery(Guid.Parse(request.ClubId));
            var club = await _mediator.Send(query);

            if (club == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Club not found"));
            }

            // Return Apollo club with subscription information
            return new GetClubResponse
            {
                Club = new ClubModel
                {
                    ClubId = club.Id.ToString(),
                    Name = club.Name,
                    Code = club.Code,
                    Country = club.Country,
                    PrimaryContactEmail = club.PrimaryContactEmail,
                    PrimaryContactName = club.PrimaryContactName,
                    SubscriptionTier = club.SubscriptionTier.ToString(),
                    MemberLimit = club.MemberLimit,
                    IsActive = club.IsActive,
                    CreatedAt = club.CreatedAt.ToString("O"),
                    Address = new AddressModel
                    {
                        Street = club.Address.Street,
                        City = club.Address.City,
                        State = club.Address.State,
                        PostalCode = club.Address.PostalCode,
                        Country = club.Address.Country
                    }
                }
            };
        }
        catch (Exception ex) when (!(ex is RpcException))
        {
            _logger.LogError(ex, "Error processing GetClub request for club {ClubId}, Correlation: {CorrelationId}",
                request.ClubId, userContext?.CorrelationId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<ValidateClubAccessResponse> ValidateClubAccess(ValidateClubAccessRequest request, ServerCallContext context)
    {
        var userContext = _userContextAccessor.Current;
        
        _logger.LogInformation("Validating club access for user: {UserId}, Club: {ClubId}, Permission: {Permission}, Correlation: {CorrelationId}",
            request.UserId, request.ClubId, request.RequiredPermission, userContext?.CorrelationId);

        try
        {
            var query = new ValidateClubAccessQuery(
                userId: Guid.Parse(request.UserId),
                clubId: Guid.Parse(request.ClubId),
                requiredPermission: request.RequiredPermission
            );

            var hasAccess = await _mediator.Send(query);

            return new ValidateClubAccessResponse
            {
                HasAccess = hasAccess,
                UserId = request.UserId,
                ClubId = request.ClubId,
                Permission = request.RequiredPermission
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating club access for user {UserId}, club {ClubId}, Correlation: {CorrelationId}",
                request.UserId, request.ClubId, userContext?.CorrelationId);
            
            return new ValidateClubAccessResponse
            {
                HasAccess = false,
                UserId = request.UserId,
                ClubId = request.ClubId,
                Permission = request.RequiredPermission
            };
        }
    }
}
```

## 5. Apollo Event Consumer Example

```csharp
using MassTransit;
using Shared.Contracts.Messages;
using Shared.Contracts.UserContext; // Add Apollo context

namespace CommunicationService.Application.Consumers;

public class MemberJoinedEventConsumer : IConsumer<MemberJoinedEvent>
{
    private readonly IUserContextAccessor _userContextAccessor; // Add Apollo context
    private readonly IEmailService _emailService;
    private readonly ILogger<MemberJoinedEventConsumer> _logger;

    public MemberJoinedEventConsumer(
        IUserContextAccessor userContextAccessor, // Add Apollo context
        IEmailService emailService,
        ILogger<MemberJoinedEventConsumer> logger)
    {
        _userContextAccessor = userContextAccessor; // Add Apollo context
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<MemberJoinedEvent> context)
    {
        // Apollo: Context is automatically extracted by UserContextConsumeFilter
        var userContext = _userContextAccessor.Current;
        
        _logger.LogInformation("Processing MemberJoinedEvent for Club: {ClubId}, Member: {MemberId}, Correlation: {CorrelationId}",
            context.Message.ClubId, context.Message.MemberId, userContext?.CorrelationId);

        try
        {
            // Apollo: Send welcome email to new club member
            await _emailService.SendWelcomeEmailAsync(new WelcomeEmailRequest
            {
                ClubId = context.Message.ClubId,
                MemberEmail = context.Message.MemberEmail,
                MemberName = context.Message.MemberName,
                ClubName = await GetClubNameAsync(context.Message.ClubId),
                MembershipType = context.Message.MembershipType,
                Sports = context.Message.Sports
            });

            _logger.LogInformation("Welcome email sent to {MemberEmail} for club {ClubId}, Correlation: {CorrelationId}",
                context.Message.MemberEmail, context.Message.ClubId, userContext?.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email for member {MemberId} in club {ClubId}, Correlation: {CorrelationId}",
                context.Message.MemberId, context.Message.ClubId, userContext?.CorrelationId);
            throw; // Re-throw to trigger retry
        }
    }

    private async Task<string> GetClubNameAsync(Guid clubId)
    {
        // Apollo: Get club name via gRPC (context will be propagated)
        var clubClient = _serviceProvider.GetRequiredService<ClubService.ClubServiceClient>();
        var response = await clubClient.GetClubAsync(new GetClubRequest { ClubId = clubId.ToString() });
        return response.Club.Name;
    }
}
```

## 6. Apollo Multi-Tenant Repository Pattern

```csharp
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.UserContext;

namespace MemberService.Infrastructure.Repositories;

public class MemberRepository : IMemberRepository
{
    private readonly MemberDbContext _context;
    private readonly IUserContextAccessor _userContextAccessor; // Add Apollo context

    public MemberRepository(MemberDbContext context, IUserContextAccessor userContextAccessor)
    {
        _context = context;
        _userContextAccessor = userContextAccessor; // Add Apollo context
    }

    public async Task<List<Member>> GetMembersAsync(CancellationToken cancellationToken = default)
    {
        var userContext = _userContextAccessor.GetRequiredContext();
        
        // Apollo: Always filter by club context for data isolation
        return await _context.Members
            .Where(m => m.ClubId == Guid.Parse(userContext.ClubId))
            .Include(m => m.EmergencyContacts)
            .ToListAsync(cancellationToken);
    }

    public async Task<Member?> GetMemberByIdAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        var userContext = _userContextAccessor.GetRequiredContext();
        
        // Apollo: Ensure member belongs to user's club
        return await _context.Members
            .Where(m => m.Id == memberId && m.ClubId == Guid.Parse(userContext.ClubId))
            .Include(m => m.EmergencyContacts)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<Member>> GetMembersBySportAsync(string sport, CancellationToken cancellationToken = default)
    {
        var userContext = _userContextAccessor.GetRequiredContext();
        
        // Apollo: Club-scoped sports member query
        return await _context.Members
            .Where(m => m.ClubId == Guid.Parse(userContext.ClubId) && 
                       m.Sports.Contains(sport))
            .Include(m => m.EmergencyContacts)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Member member, CancellationToken cancellationToken = default)
    {
        var userContext = _userContextAccessor.GetRequiredContext();
        
        // Apollo: Ensure member is being added to the correct club
        if (member.ClubId.ToString() != userContext.ClubId)
        {
            throw new InvalidOperationException("Cannot add member to different club than current context");
        }

        _context.Members.Add(member);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

## 7. Apollo Health Check with Context

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shared.Contracts.UserContext;

namespace MemberService.Infrastructure.HealthChecks;

public class ApolloContextHealthCheck : IHealthCheck
{
    private readonly IUserContextAccessor _userContextAccessor;

    public ApolloContextHealthCheck(IUserContextAccessor userContextAccessor)
    {
        _userContextAccessor = userContextAccessor;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var userContext = _userContextAccessor.Current;
        
        var data = new Dictionary<string, object>
        {
            ["HasUserContext"] = userContext != null,
            ["UserId"] = userContext?.UserId ?? "None",
            ["ClubId"] = userContext?.ClubId ?? "None",
            ["Roles"] = userContext?.Roles?.Count ?? 0,
            ["CorrelationId"] = userContext?.CorrelationId ?? "None"
        };

        var isHealthy = userContext != null || context.Registration.Name.Contains("background");
        
        return Task.FromResult(isHealthy 
            ? HealthCheckResult.Healthy("Apollo User Context is working", data)
            : HealthCheckResult.Degraded("Apollo User Context not available", data));
    }
}
```

## 8. Apollo Configuration Summary

Key points for integrating User Context in Apollo services:

1. **Multi-Tenant Isolation**: Always filter by `ClubId` from user context
2. **Club Access Validation**: Verify user permissions for club operations
3. **Context Propagation**: Automatic context flow between Apollo services
4. **Sports-Specific Data**: Include sports information in member operations
5. **Event Publishing**: Apollo events include club context automatically
6. **Security**: Validate club access before any data operations
7. **Audit Trails**: Log all operations with user and club context

---

**Apollo** - Empowering sports clubs with secure, multi-tenant technology 🚀

For support: [support@apollo-sports.com](mailto:support@apollo-sports.com) 
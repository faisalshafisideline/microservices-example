# Article Service - User Context Integration Example

This example shows how to integrate the User Context system into the Article Service.

## 1. Update ArticleService.csproj

Add reference to Shared.Contracts:

```xml
<ItemGroup>
  <ProjectReference Include="..\Shared\Contracts\Shared.Contracts.csproj" />
</ItemGroup>
```

## 2. Update Program.cs

```csharp
using ArticleService.Application.Common;
using ArticleService.Application.Repositories;
using ArticleService.Infrastructure;
using ArticleService.Infrastructure.Data;
using ArticleService.Infrastructure.Repositories;
using ArticleService.Services;
using Carter;
using FluentValidation;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Reflection;
// Add these imports
using Shared.Contracts.UserContext.Extensions;
using Shared.Contracts.UserContext.Interceptors;

var builder = WebApplication.CreateBuilder(args);

// ... existing code ...

// Add User Context services
builder.Services.AddUserContext();
builder.Services.AddUserContextGrpcInterceptors();

// Update gRPC configuration
builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<UserContextServerInterceptor>();
});

// Update MassTransit configuration
builder.Services.AddMassTransit(x =>
{
    x.ConfigureUserContextPropagation(); // Add this line
    
    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitMqHost = builder.Configuration.GetValue<string>("RabbitMQ:Host") ?? "localhost";
        var rabbitMqUsername = builder.Configuration.GetValue<string>("RabbitMQ:Username") ?? "guest";
        var rabbitMqPassword = builder.Configuration.GetValue<string>("RabbitMQ:Password") ?? "guest";

        cfg.Host(rabbitMqHost, h =>
        {
            h.Username(rabbitMqUsername);
            h.Password(rabbitMqPassword);
        });

        cfg.ConfigureUserContextPropagation(context); // Add this line
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

// Add User Context middleware
app.UseUserContext(); // Add this after authentication (if any)

// ... rest of the configuration ...
```

## 3. Update CreateArticleCommandHandler

```csharp
using ArticleService.Application.Commands;
using ArticleService.Application.Common;
using ArticleService.Application.Repositories;
using ArticleService.Domain.Entities;
using ArticleService.Domain.ValueObjects;
using MediatR;
using MassTransit;
using Shared.Contracts.Messages;
using Shared.Contracts.UserContext; // Add this

namespace ArticleService.Application.Commands;

public class CreateArticleCommandHandler : IRequestHandler<CreateArticleCommand, Guid>
{
    private readonly IArticleRepository _articleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IUserContextAccessor _userContextAccessor; // Add this
    private readonly ILogger<CreateArticleCommandHandler> _logger;

    public CreateArticleCommandHandler(
        IArticleRepository articleRepository,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint,
        IUserContextAccessor userContextAccessor, // Add this
        ILogger<CreateArticleCommandHandler> logger)
    {
        _articleRepository = articleRepository;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
        _userContextAccessor = userContextAccessor; // Add this
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateArticleCommand request, CancellationToken cancellationToken)
    {
        // Get user context
        var userContext = _userContextAccessor.GetCurrentOrEmpty();
        
        _logger.LogInformation("Creating article for user: {UserId}, Correlation: {CorrelationId}",
            userContext.UserId, userContext.CorrelationId);

        var article = new Article
        {
            Id = ArticleId.New(),
            Title = request.Title,
            Content = request.Content,
            Metadata = new ArticleMetadata
            {
                AuthorId = userContext.UserId ?? "anonymous", // Use context
                CreatedAt = DateTime.UtcNow,
                Tags = request.Tags
            }
        };

        await _articleRepository.AddAsync(article, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish event - context will be automatically propagated
        await _publishEndpoint.Publish(new ArticleCreatedEvent
        {
            ArticleId = article.Id.Value,
            Title = article.Title,
            Content = article.Content,
            AuthorId = article.Metadata.AuthorId,
            CreatedAt = article.Metadata.CreatedAt,
            Tags = article.Metadata.Tags
        }, cancellationToken);

        _logger.LogInformation("Article created: {ArticleId}, Correlation: {CorrelationId}",
            article.Id.Value, userContext.CorrelationId);

        return article.Id.Value;
    }
}
```

## 4. Update ArticleGrpcService

```csharp
using ArticleService.Application.Queries;
using Grpc.Core;
using MediatR;
using Shared.Contracts.Protos;
using Shared.Contracts.UserContext; // Add this

namespace ArticleService.Services;

public class ArticleGrpcService : Shared.Contracts.Protos.ArticleService.ArticleServiceBase
{
    private readonly IMediator _mediator;
    private readonly IUserContextAccessor _userContextAccessor; // Add this
    private readonly ILogger<ArticleGrpcService> _logger;

    public ArticleGrpcService(
        IMediator mediator, 
        IUserContextAccessor userContextAccessor, // Add this
        ILogger<ArticleGrpcService> logger)
    {
        _mediator = mediator;
        _userContextAccessor = userContextAccessor; // Add this
        _logger = logger;
    }

    public override async Task<GetArticleResponse> GetArticle(GetArticleRequest request, ServerCallContext context)
    {
        // Context is automatically extracted by UserContextServerInterceptor
        var userContext = _userContextAccessor.Current;
        
        _logger.LogInformation("Processing gRPC GetArticle request for user: {UserId}, Correlation: {CorrelationId}",
            userContext?.UserId, userContext?.CorrelationId);

        try
        {
            var query = new GetArticleQuery(Guid.Parse(request.ArticleId));
            var article = await _mediator.Send(query);

            if (article == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Article not found"));
            }

            return new GetArticleResponse
            {
                ArticleId = article.Id.ToString(),
                Title = article.Title,
                Content = article.Content,
                AuthorId = article.AuthorId,
                CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(article.CreatedAt),
                Tags = { article.Tags }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing gRPC GetArticle request, Correlation: {CorrelationId}",
                userContext?.CorrelationId);
            throw;
        }
    }
}
```

## 5. Update Article Endpoints (Carter)

```csharp
using ArticleService.Application.Commands;
using ArticleService.Application.Queries;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.UserContext; // Add this

namespace ArticleService.Endpoints;

public class ArticleEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/articles")
            .WithTags("Articles")
            .WithOpenApi();

        group.MapPost("/", CreateArticle)
            .WithName("CreateArticle")
            .WithSummary("Create a new article");

        group.MapGet("/{id:guid}", GetArticle)
            .WithName("GetArticle")
            .WithSummary("Get article by ID");
    }

    private static async Task<IResult> CreateArticle(
        [FromBody] CreateArticleCommand command,
        IMediator mediator,
        IUserContextAccessor userContextAccessor, // Add this
        ILogger<ArticleEndpoints> logger)
    {
        var userContext = userContextAccessor.GetCurrentOrEmpty();
        
        logger.LogInformation("Creating article via HTTP for user: {UserId}, Correlation: {CorrelationId}",
            userContext.UserId, userContext.CorrelationId);

        try
        {
            var articleId = await mediator.Send(command);
            return Results.Created($"/api/articles/{articleId}", new { ArticleId = articleId });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating article, Correlation: {CorrelationId}",
                userContext.CorrelationId);
            return Results.Problem("An error occurred while creating the article");
        }
    }

    private static async Task<IResult> GetArticle(
        Guid id,
        IMediator mediator,
        IUserContextAccessor userContextAccessor, // Add this
        ILogger<ArticleEndpoints> logger)
    {
        var userContext = userContextAccessor.GetCurrentOrEmpty();
        
        logger.LogInformation("Getting article {ArticleId} via HTTP for user: {UserId}, Correlation: {CorrelationId}",
            id, userContext.UserId, userContext.CorrelationId);

        try
        {
            var query = new GetArticleQuery(id);
            var article = await mediator.Send(query);

            if (article == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(article);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting article {ArticleId}, Correlation: {CorrelationId}",
                id, userContext.CorrelationId);
            return Results.Problem("An error occurred while retrieving the article");
        }
    }
}
```

## 6. Testing the Integration

### Test via API Gateway

```bash
# Create an article through the gateway
curl -X POST "http://localhost:5000/api/articles" \
  -H "Authorization: Basic YWRtaW46c3VwZXJzZWNyZXQ=" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: test-correlation-123" \
  -d '{
    "title": "Test Article",
    "content": "This is a test article",
    "tags": ["test", "example"]
  }'
```

### Expected Log Output

```
[INFO] User context enriched for gateway request: User: admin (admin), Roles: [Admin, Reporter, User], CorrelationId: test-correlation-123
[INFO] Creating article for user: admin, Correlation: test-correlation-123
[INFO] Injected user context into published message: ArticleCreatedEvent, CorrelationId: test-correlation-123
[INFO] Article created: 12345678-1234-1234-1234-123456789012, Correlation: test-correlation-123
```

## 7. Key Benefits

1. **Automatic Context Propagation**: User context flows seamlessly from HTTP → gRPC → RabbitMQ
2. **Correlation Tracking**: Every operation can be traced using the correlation ID
3. **Security Context**: User ID and roles are available throughout the request lifecycle
4. **Audit Trail**: All operations are logged with user context for compliance
5. **Multi-tenant Support**: Tenant ID can be propagated for multi-tenant scenarios

## 8. Best Practices Applied

- Context is injected via DI, making it testable
- Graceful fallback to empty context for anonymous operations
- Comprehensive logging with correlation IDs
- Automatic cleanup of context after request completion
- Type-safe access to context properties 
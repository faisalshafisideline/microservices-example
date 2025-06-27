using ArticleService.Application.Common;
using ArticleService.Application.DTOs;
using ArticleService.Application.Mappers;
using ArticleService.Application.Repositories;
using ArticleService.Domain.Entities;
using ArticleService.Domain.ValueObjects;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Messages;
// using Shared.Contracts.UserContext;
// using Shared.Contracts.Observability;
// using Shared.Contracts.Resilience;
// using Shared.Contracts.Caching;
// using Shared.Contracts.Security;
// using System.Diagnostics;

namespace ArticleService.Application.Commands;

public sealed class CreateArticleCommandHandler : IRequestHandler<CreateArticleCommand, ArticleDto>
{
    private readonly IArticleRepository _articleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CreateArticleCommandHandler> _logger;
    // private readonly IUserContextAccessor _userContextAccessor;
    // private readonly IBusinessMetricsCollector _metricsCollector;
    // private readonly IResilienceService _resilienceService;
    // private readonly IDistributedCacheService _cacheService;
    // private readonly IRateLimitingService _rateLimitingService;

    public CreateArticleCommandHandler(
        IArticleRepository articleRepository,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint,
        ILogger<CreateArticleCommandHandler> logger)
        // IUserContextAccessor userContextAccessor,
        // IBusinessMetricsCollector metricsCollector,
        // IResilienceService resilienceService,
        // IDistributedCacheService cacheService,
        // IRateLimitingService rateLimitingService)
    {
        _articleRepository = articleRepository ?? throw new ArgumentNullException(nameof(articleRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        // _userContextAccessor = userContextAccessor ?? throw new ArgumentNullException(nameof(userContextAccessor));
        // _metricsCollector = metricsCollector ?? throw new ArgumentNullException(nameof(metricsCollector));
        // _resilienceService = resilienceService ?? throw new ArgumentNullException(nameof(resilienceService));
        // _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        // _rateLimitingService = rateLimitingService ?? throw new ArgumentNullException(nameof(rateLimitingService));
    }

    public async Task<ArticleDto> Handle(CreateArticleCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating article with title: {Title}", request.Title);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            // Create the article entity
            var articleId = ArticleId.New();
            var article = new Article(
                articleId,
                request.Title,
                request.Content,
                new AuthorId(request.AuthorId),
                request.AuthorName,
                request.Tags);

            // Set metadata if provided
            if (!string.IsNullOrWhiteSpace(request.Category) || 
                !string.IsNullOrWhiteSpace(request.Summary) || 
                !string.IsNullOrWhiteSpace(request.FeaturedImageUrl))
            {
                var metadata = ArticleMetadata.Create(
                    request.Category,
                    request.Summary,
                    request.FeaturedImageUrl,
                    request.Content);
                article.SetMetadata(metadata);
            }

            // Publish immediately if requested
            if (request.PublishImmediately)
            {
                article.Publish();
            }

            // Save to repository
            await _articleRepository.AddAsync(article, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Publish domain event
            var articleCreatedEvent = ArticleCreatedEvent.Create(
                article.Id,
                article.Title,
                article.AuthorId,
                article.AuthorName,
                article.Metadata?.Category,
                article.Tags?.ToArray(),
                article.Metadata?.EstimatedReadTimeMinutes ?? 0);

            await _publishEndpoint.Publish(articleCreatedEvent, cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Successfully created article with ID: {ArticleId}", article.Id);

            return article.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating article with title: {Title}", request.Title);
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
} 
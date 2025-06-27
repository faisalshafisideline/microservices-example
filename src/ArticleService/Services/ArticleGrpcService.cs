using ArticleService.Application.Queries;
using Grpc.Core;
using MediatR;
using Shared.Contracts.Grpc;
using Google.Protobuf.WellKnownTypes;

namespace ArticleService.Services;

public sealed class ArticleGrpcService : Shared.Contracts.Grpc.ArticleService.ArticleServiceBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ArticleGrpcService> _logger;

    public ArticleGrpcService(IMediator mediator, ILogger<ArticleGrpcService> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task<GetArticleResponse> GetArticle(GetArticleRequest request, ServerCallContext context)
    {
        _logger.LogDebug("gRPC GetArticle called for ID: {ArticleId}", request.ArticleId);

        try
        {
            var query = new GetArticleQuery(request.ArticleId);
            var article = await _mediator.Send(query, context.CancellationToken);

            if (article == null)
            {
                return new GetArticleResponse { Found = false };
            }

            return new GetArticleResponse
            {
                Found = true,
                Article = new Article
                {
                    Id = article.Id,
                    Title = article.Title,
                    Content = article.Content,
                    AuthorId = article.AuthorId,
                    AuthorName = article.AuthorName,
                    CreatedAt = Timestamp.FromDateTimeOffset(article.CreatedAt),
                    UpdatedAt = Timestamp.FromDateTimeOffset(article.UpdatedAt),
                    Status = MapArticleStatus(article.Status),
                    Tags = { article.Tags },
                    Metadata = article.Metadata != null ? new ArticleMetadata
                    {
                        Category = article.Metadata.Category ?? string.Empty,
                        EstimatedReadTimeMinutes = article.Metadata.EstimatedReadTimeMinutes,
                        Summary = article.Metadata.Summary ?? string.Empty,
                        FeaturedImageUrl = article.Metadata.FeaturedImageUrl ?? string.Empty
                    } : null
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC GetArticle for ID: {ArticleId}", request.ArticleId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<GetArticlesResponse> GetArticles(GetArticlesRequest request, ServerCallContext context)
    {
        _logger.LogDebug("gRPC GetArticles called for {Count} articles", request.ArticleIds.Count);

        try
        {
            var query = new GetArticlesQuery(
                request.ArticleIds.ToArray(),
                request.PageSize > 0 ? request.PageSize : 10,
                string.IsNullOrEmpty(request.PageToken) ? null : request.PageToken);

            var result = await _mediator.Send(query, context.CancellationToken);

            var response = new GetArticlesResponse
            {
                TotalCount = result.TotalCount,
                NextPageToken = result.NextPageToken ?? string.Empty
            };

            foreach (var article in result.Articles)
            {
                response.Articles.Add(new Article
                {
                    Id = article.Id,
                    Title = article.Title,
                    Content = article.Content,
                    AuthorId = article.AuthorId,
                    AuthorName = article.AuthorName,
                    CreatedAt = Timestamp.FromDateTimeOffset(article.CreatedAt),
                    UpdatedAt = Timestamp.FromDateTimeOffset(article.UpdatedAt),
                    Status = MapArticleStatus(article.Status),
                    Tags = { article.Tags },
                    Metadata = article.Metadata != null ? new ArticleMetadata
                    {
                        Category = article.Metadata.Category ?? string.Empty,
                        EstimatedReadTimeMinutes = article.Metadata.EstimatedReadTimeMinutes,
                        Summary = article.Metadata.Summary ?? string.Empty,
                        FeaturedImageUrl = article.Metadata.FeaturedImageUrl ?? string.Empty
                    } : null
                });
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC GetArticles");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    private static ArticleStatus MapArticleStatus(string status) => status.ToLowerInvariant() switch
    {
        "draft" => ArticleStatus.Draft,
        "published" => ArticleStatus.Published,
        "archived" => ArticleStatus.Archived,
        _ => ArticleStatus.Unspecified
    };
} 
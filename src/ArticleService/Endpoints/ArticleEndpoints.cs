using ArticleService.Application.Commands;
using ArticleService.Application.Queries;
using Carter;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ArticleService.Endpoints;

public sealed class ArticleEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/articles")
            .WithTags("Articles")
            .WithOpenApi();

        group.MapPost("/", CreateArticle)
            .WithName("CreateArticle")
            .WithSummary("Create a new article")
            .Produces<ArticleResponse>(201)
            .ProducesValidationProblem();

        group.MapGet("/{id}", GetArticle)
            .WithName("GetArticle")
            .WithSummary("Get an article by ID")
            .Produces<ArticleResponse>()
            .Produces(404);

        group.MapGet("/", GetArticles)
            .WithName("GetArticles")
            .WithSummary("Get multiple articles")
            .Produces<ArticlesPageResponse>();
    }

    private static async Task<IResult> CreateArticle(
        [FromBody] CreateArticleRequest request,
        IMediator mediator,
        IValidator<CreateArticleRequest> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var command = new CreateArticleCommand
        {
            Title = request.Title,
            Content = request.Content,
            AuthorId = request.AuthorId,
            AuthorName = request.AuthorName,
            Category = request.Category,
            Tags = request.Tags,
            Summary = request.Summary,
            FeaturedImageUrl = request.FeaturedImageUrl,
            PublishImmediately = request.PublishImmediately
        };

        var article = await mediator.Send(command, cancellationToken);
        
        var response = new ArticleResponse
        {
            Id = article.Id,
            Title = article.Title,
            Content = article.Content,
            AuthorId = article.AuthorId,
            AuthorName = article.AuthorName,
            Status = article.Status,
            CreatedAt = article.CreatedAt,
            UpdatedAt = article.UpdatedAt,
            Tags = article.Tags,
            Metadata = article.Metadata != null ? new ArticleMetadataResponse
            {
                Category = article.Metadata.Category,
                EstimatedReadTimeMinutes = article.Metadata.EstimatedReadTimeMinutes,
                Summary = article.Metadata.Summary,
                FeaturedImageUrl = article.Metadata.FeaturedImageUrl
            } : null
        };

        return Results.Created($"/api/articles/{article.Id}", response);
    }

    private static async Task<IResult> GetArticle(
        string id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetArticleQuery(id);
        var article = await mediator.Send(query, cancellationToken);

        if (article == null)
        {
            return Results.NotFound();
        }

        var response = new ArticleResponse
        {
            Id = article.Id,
            Title = article.Title,
            Content = article.Content,
            AuthorId = article.AuthorId,
            AuthorName = article.AuthorName,
            Status = article.Status,
            CreatedAt = article.CreatedAt,
            UpdatedAt = article.UpdatedAt,
            Tags = article.Tags,
            Metadata = article.Metadata != null ? new ArticleMetadataResponse
            {
                Category = article.Metadata.Category,
                EstimatedReadTimeMinutes = article.Metadata.EstimatedReadTimeMinutes,
                Summary = article.Metadata.Summary,
                FeaturedImageUrl = article.Metadata.FeaturedImageUrl
            } : null
        };

        return Results.Ok(response);
    }

    private static async Task<IResult> GetArticles(
        IMediator mediator,
        CancellationToken cancellationToken,
        [FromQuery] string[]? ids = null,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? pageToken = null)
    {
        var query = new GetArticlesQuery(
            ids ?? [],
            Math.Min(pageSize, 100), // Limit page size
            pageToken);

        var result = await mediator.Send(query, cancellationToken);

        var response = new ArticlesPageResponse
        {
            Articles = result.Articles.Select(a => new ArticleResponse
            {
                Id = a.Id,
                Title = a.Title,
                Content = a.Content,
                AuthorId = a.AuthorId,
                AuthorName = a.AuthorName,
                Status = a.Status,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt,
                Tags = a.Tags,
                Metadata = a.Metadata != null ? new ArticleMetadataResponse
                {
                    Category = a.Metadata.Category,
                    EstimatedReadTimeMinutes = a.Metadata.EstimatedReadTimeMinutes,
                    Summary = a.Metadata.Summary,
                    FeaturedImageUrl = a.Metadata.FeaturedImageUrl
                } : null
            }).ToArray(),
            TotalCount = result.TotalCount,
            NextPageToken = result.NextPageToken
        };

        return Results.Ok(response);
    }
}

// Request/Response models
public sealed record CreateArticleRequest
{
    [Required]
    public required string Title { get; init; }

    [Required]
    public required string Content { get; init; }

    [Required]
    public required string AuthorId { get; init; }

    [Required]
    public required string AuthorName { get; init; }

    public string? Category { get; init; }
    public string[]? Tags { get; init; }
    public string? Summary { get; init; }
    public string? FeaturedImageUrl { get; init; }
    public bool PublishImmediately { get; init; } = false;
}

public sealed record ArticleResponse
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Content { get; init; }
    public required string AuthorId { get; init; }
    public required string AuthorName { get; init; }
    public required string Status { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public string[] Tags { get; init; } = [];
    public ArticleMetadataResponse? Metadata { get; init; }
}

public sealed record ArticleMetadataResponse
{
    public string? Category { get; init; }
    public int EstimatedReadTimeMinutes { get; init; }
    public string? Summary { get; init; }
    public string? FeaturedImageUrl { get; init; }
}

public sealed record ArticlesPageResponse
{
    public ArticleResponse[] Articles { get; init; } = [];
    public int TotalCount { get; init; }
    public string? NextPageToken { get; init; }
}

public sealed class CreateArticleRequestValidator : AbstractValidator<CreateArticleRequest>
{
    public CreateArticleRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(50000);

        RuleFor(x => x.AuthorId)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.AuthorName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Category)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.Category));

        RuleFor(x => x.Summary)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrEmpty(x.Summary));

        RuleFor(x => x.FeaturedImageUrl)
            .Must(BeAValidUrl)
            .When(x => !string.IsNullOrEmpty(x.FeaturedImageUrl))
            .WithMessage("Featured image URL must be a valid URL");

        RuleFor(x => x.Tags)
            .Must(tags => tags == null || tags.Length <= 10)
            .WithMessage("Maximum 10 tags allowed");
    }

    private static bool BeAValidUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }
} 
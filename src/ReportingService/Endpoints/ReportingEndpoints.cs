using Carter;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReportingService.Application.Services;
using ReportingService.Domain.ValueObjects;
using ReportingService.Infrastructure.Data;
using Shared.Contracts.Messages;
using System.ComponentModel.DataAnnotations;

namespace ReportingService.Endpoints;

public sealed class ReportingEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reporting")
            .WithTags("Reporting")
            .WithOpenApi();

        group.MapGet("/articles/{articleId}/views", GetArticleViews)
            .WithName("GetArticleViews")
            .WithSummary("Get view statistics for an article")
            .Produces<ArticleViewsResponse>()
            .Produces(404);

        group.MapPost("/articles/{articleId}/views", RecordArticleView)
            .WithName("RecordArticleView")
            .WithSummary("Record a view for an article")
            .Produces(200)
            .Produces(404);

        group.MapGet("/articles/{articleId}/created-date", GetArticleCreatedDate)
            .WithName("GetArticleCreatedDate")
            .WithSummary("Get article creation date from Article Service via gRPC")
            .Produces<ArticleCreatedDateResponse>()
            .Produces(404);

        group.MapGet("/articles/top-viewed", GetTopViewedArticles)
            .WithName("GetTopViewedArticles")
            .WithSummary("Get top viewed articles")
            .Produces<TopViewedArticlesResponse>();

        group.MapGet("/authors/{authorId}/stats", GetAuthorStats)
            .WithName("GetAuthorStats")
            .WithSummary("Get statistics for an author")
            .Produces<AuthorStatsResponse>()
            .Produces(404);
    }

    private static async Task<IResult> GetArticleViews(
        string articleId,
        ReportingDbContext context,
        CancellationToken cancellationToken)
    {
        var report = await context.ArticleReports
            .FirstOrDefaultAsync(ar => ar.ArticleId == new ArticleId(articleId), cancellationToken);

        if (report == null)
        {
            return Results.NotFound();
        }

        var response = new ArticleViewsResponse
        {
            ArticleId = report.ArticleId,
            Title = report.Title,
            ViewCount = report.ViewCount,
            LastViewedAt = report.LastViewedAt,
            CreatedAt = report.CreatedAt
        };

        return Results.Ok(response);
    }

    private static async Task<IResult> RecordArticleView(
        string articleId,
        [FromBody] RecordViewRequest? request,
        ReportingDbContext context,
        IPublisher publisher,
        CancellationToken cancellationToken)
    {
        var report = await context.ArticleReports
            .FirstOrDefaultAsync(ar => ar.ArticleId == new ArticleId(articleId), cancellationToken);

        if (report == null)
        {
            return Results.NotFound();
        }

        // Record the view
        report.RecordView();
        await context.SaveChangesAsync(cancellationToken);

        // Publish ArticleViewedEvent for further analytics
        var viewedEvent = ArticleViewedEvent.Create(
            articleId,
            request?.UserId,
            request?.SessionId,
            request?.UserAgent,
            request?.IpAddress,
            request?.Referrer);

        await publisher.Publish(viewedEvent, cancellationToken);

        return Results.Ok();
    }

    private static async Task<IResult> GetArticleCreatedDate(
        string articleId,
        IArticleGrpcClient articleClient,
        CancellationToken cancellationToken)
    {
        var articleResponse = await articleClient.GetArticleAsync(articleId, cancellationToken);

        if (articleResponse?.Found != true || articleResponse.Article == null)
        {
            return Results.NotFound();
        }

        var response = new ArticleCreatedDateResponse
        {
            ArticleId = articleResponse.Article.Id,
            Title = articleResponse.Article.Title,
            AuthorName = articleResponse.Article.AuthorName,
            CreatedAt = articleResponse.Article.CreatedAt.ToDateTimeOffset()
        };

        return Results.Ok(response);
    }

    private static async Task<IResult> GetTopViewedArticles(
        ReportingDbContext context,
        CancellationToken cancellationToken,
        [FromQuery] int limit = 10,
        [FromQuery] string? category = null,
        [FromQuery] string? authorId = null)
    {
        var query = context.ArticleReports.AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(ar => ar.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(authorId))
        {
            query = query.Where(ar => ar.AuthorId == authorId);
        }

        var topArticles = await query
            .OrderByDescending(ar => ar.ViewCount)
            .Take(Math.Min(limit, 100))
            .Select(ar => new TopViewedArticleInfo
            {
                ArticleId = ar.ArticleId,
                Title = ar.Title,
                AuthorName = ar.AuthorName,
                Category = ar.Category,
                ViewCount = ar.ViewCount,
                CreatedAt = ar.CreatedAt,
                LastViewedAt = ar.LastViewedAt
            })
            .ToListAsync(cancellationToken);

        var response = new TopViewedArticlesResponse
        {
            Articles = topArticles,
            TotalCount = topArticles.Count
        };

        return Results.Ok(response);
    }

    private static async Task<IResult> GetAuthorStats(
        string authorId,
        ReportingDbContext context,
        CancellationToken cancellationToken)
    {
        var authorReports = await context.ArticleReports
            .Where(ar => ar.AuthorId == authorId)
            .ToListAsync(cancellationToken);

        if (!authorReports.Any())
        {
            return Results.NotFound();
        }

        var response = new AuthorStatsResponse
        {
            AuthorId = authorId,
            AuthorName = authorReports.First().AuthorName,
            TotalArticles = authorReports.Count,
            TotalViews = authorReports.Sum(ar => ar.ViewCount),
            AverageViewsPerArticle = authorReports.Average(ar => ar.ViewCount),
            MostViewedArticle = authorReports.OrderByDescending(ar => ar.ViewCount).First().Title,
            Categories = authorReports.Where(ar => !string.IsNullOrEmpty(ar.Category))
                                   .GroupBy(ar => ar.Category)
                                   .Select(g => new CategoryStats
                                   {
                                       Category = g.Key!,
                                       ArticleCount = g.Count(),
                                       TotalViews = g.Sum(ar => ar.ViewCount)
                                   })
                                   .ToList()
        };

        return Results.Ok(response);
    }
}

// Request/Response models
public sealed record RecordViewRequest
{
    public string? UserId { get; init; }
    public string? SessionId { get; init; }
    public string? UserAgent { get; init; }
    public string? IpAddress { get; init; }
    public string? Referrer { get; init; }
}

public sealed record ArticleViewsResponse
{
    public required string ArticleId { get; init; }
    public required string Title { get; init; }
    public int ViewCount { get; init; }
    public DateTime LastViewedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed record ArticleCreatedDateResponse
{
    public required string ArticleId { get; init; }
    public required string Title { get; init; }
    public required string AuthorName { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record TopViewedArticlesResponse
{
    public List<TopViewedArticleInfo> Articles { get; init; } = [];
    public int TotalCount { get; init; }
}

public sealed record TopViewedArticleInfo
{
    public required string ArticleId { get; init; }
    public required string Title { get; init; }
    public required string AuthorName { get; init; }
    public string? Category { get; init; }
    public int ViewCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastViewedAt { get; init; }
}

public sealed record AuthorStatsResponse
{
    public required string AuthorId { get; init; }
    public required string AuthorName { get; init; }
    public int TotalArticles { get; init; }
    public int TotalViews { get; init; }
    public double AverageViewsPerArticle { get; init; }
    public required string MostViewedArticle { get; init; }
    public List<CategoryStats> Categories { get; init; } = [];
}

public sealed record CategoryStats
{
    public required string Category { get; init; }
    public int ArticleCount { get; init; }
    public int TotalViews { get; init; }
}

// Simple publisher interface for MassTransit
public interface IPublisher
{
    Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : class;
} 
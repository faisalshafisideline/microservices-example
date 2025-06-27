using ArticleService.Application.DTOs;
using MediatR;

namespace ArticleService.Application.Commands;

public sealed record CreateArticleCommand : IRequest<ArticleDto>
{
    public required string Title { get; init; }
    public required string Content { get; init; }
    public required string AuthorId { get; init; }
    public required string AuthorName { get; init; }
    public string? Category { get; init; }
    public string[]? Tags { get; init; }
    public string? Summary { get; init; }
    public string? FeaturedImageUrl { get; init; }
    public bool PublishImmediately { get; init; } = false;
} 
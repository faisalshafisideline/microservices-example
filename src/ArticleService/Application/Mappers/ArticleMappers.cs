using ArticleService.Application.DTOs;
using ArticleService.Domain.Entities;
using ArticleService.Domain.ValueObjects;

namespace ArticleService.Application.Mappers;

public static class ArticleMappers
{
    public static ArticleDto ToDto(this Article article)
    {
        return new ArticleDto
        {
            Id = article.Id.Value,
            Title = article.Title,
            Content = article.Content,
            AuthorId = article.AuthorId.Value,
            AuthorName = article.AuthorName,
            Status = article.Status.ToString(),
            CreatedAt = DateTime.SpecifyKind(article.CreatedAt, DateTimeKind.Utc),
            UpdatedAt = DateTime.SpecifyKind(article.UpdatedAt, DateTimeKind.Utc),
            Tags = article.Tags.ToArray(),
            Metadata = article.Metadata?.ToDto()
        };
    }

    public static ArticleMetadataDto ToDto(this ArticleMetadata metadata)
    {
        return new ArticleMetadataDto
        {
            Category = metadata.Category,
            EstimatedReadTimeMinutes = metadata.EstimatedReadTimeMinutes,
            Summary = metadata.Summary,
            FeaturedImageUrl = metadata.FeaturedImageUrl
        };
    }
} 
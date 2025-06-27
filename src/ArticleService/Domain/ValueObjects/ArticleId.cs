namespace ArticleService.Domain.ValueObjects;

public sealed record ArticleId
{
    public string Value { get; }

    public ArticleId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("ArticleId cannot be null or empty", nameof(value));

        Value = value;
    }

    public static ArticleId New() => new(Guid.NewGuid().ToString());

    public static implicit operator string(ArticleId articleId) => articleId.Value;
    public static implicit operator ArticleId(string value) => new(value);

    public override string ToString() => Value;
}

public sealed record AuthorId
{
    public string Value { get; }

    public AuthorId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("AuthorId cannot be null or empty", nameof(value));

        Value = value;
    }

    public static AuthorId New() => new(Guid.NewGuid().ToString());

    public static implicit operator string(AuthorId authorId) => authorId.Value;
    public static implicit operator AuthorId(string value) => new(value);

    public override string ToString() => Value;
} 
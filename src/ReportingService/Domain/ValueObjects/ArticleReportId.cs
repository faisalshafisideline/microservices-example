namespace ReportingService.Domain.ValueObjects;

public sealed record ArticleReportId
{
    public string Value { get; }

    public ArticleReportId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("ArticleReportId cannot be null or empty", nameof(value));

        Value = value;
    }

    public static ArticleReportId New() => new(Guid.NewGuid().ToString());

    public static implicit operator string(ArticleReportId id) => id.Value;
    public static implicit operator ArticleReportId(string value) => new(value);

    public override string ToString() => Value;
}

public sealed record ArticleId
{
    public string Value { get; }

    public ArticleId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("ArticleId cannot be null or empty", nameof(value));

        Value = value;
    }

    public static implicit operator string(ArticleId id) => id.Value;
    public static implicit operator ArticleId(string value) => new(value);

    public override string ToString() => Value;
} 
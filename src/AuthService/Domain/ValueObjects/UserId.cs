namespace AuthService.Domain.ValueObjects;

public record UserId(Guid Value)
{
    public static UserId New() => new(Guid.NewGuid());
    public static UserId From(Guid id) => new(id);
    public static implicit operator Guid(UserId userId) => userId.Value;
    public override string ToString() => Value.ToString();
} 
namespace ClubService.Domain.ValueObjects;

public record ClubId(Guid Value)
{
    public static ClubId New() => new(Guid.NewGuid());
    public static ClubId From(Guid value) => new(value);
    public static ClubId From(string value) => new(Guid.Parse(value));
    
    public override string ToString() => Value.ToString();
    
    public static implicit operator Guid(ClubId clubId) => clubId.Value;
    public static implicit operator string(ClubId clubId) => clubId.Value.ToString();
} 
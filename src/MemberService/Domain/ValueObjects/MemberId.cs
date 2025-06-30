namespace MemberService.Domain.ValueObjects;

public record MemberId(Guid Value)
{
    public static MemberId New() => new(Guid.NewGuid());
    public static MemberId From(Guid value) => new(value);
    public static MemberId From(string value) => new(Guid.Parse(value));
    
    public override string ToString() => Value.ToString();
    
    public static implicit operator Guid(MemberId memberId) => memberId.Value;
    public static implicit operator string(MemberId memberId) => memberId.Value.ToString();
} 
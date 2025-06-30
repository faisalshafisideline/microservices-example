namespace Shared.Contracts.Messages;

public record MemberJoinedEvent(
    Guid MemberId,
    Guid ClubId,
    string Email,
    string FirstName,
    string LastName,
    string PreferredLanguage,
    string MembershipType,
    DateTime JoinedAt,
    string InvitedBy
); 
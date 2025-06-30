namespace Shared.Contracts.Messages;

public record ClubCreatedEvent(
    Guid ClubId,
    string ClubName,
    string ClubCode,
    string Country,
    string Currency,
    string Timezone,
    string DefaultLanguage,
    decimal DefaultVatRate,
    string PrimaryContactEmail,
    string PrimaryContactName,
    DateTime CreatedAt,
    string CreatedBy
); 
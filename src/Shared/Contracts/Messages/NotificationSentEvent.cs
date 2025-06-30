namespace Shared.Contracts.Messages;

public record NotificationSentEvent(
    Guid NotificationId,
    Guid ClubId,
    Guid? MemberId,
    string NotificationType, // Email, SMS, Push
    string Channel, // email address, phone number, device token
    string Subject,
    string Language,
    string Status, // Sent, Failed, Delivered
    DateTime SentAt,
    string? ErrorMessage
); 
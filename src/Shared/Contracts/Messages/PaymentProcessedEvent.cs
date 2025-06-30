namespace Shared.Contracts.Messages;

public record PaymentProcessedEvent(
    Guid PaymentId,
    Guid ClubId,
    Guid MemberId,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    string InvoiceNumber,
    string Status,
    DateTime ProcessedAt,
    string TransactionId
); 
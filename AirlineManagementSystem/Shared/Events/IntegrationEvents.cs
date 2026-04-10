namespace Shared.Events;

public abstract record IntegrationEvent
{
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

public record BookingCreatedEvent(
    int BookingId,
    int UserId,
    int FlightId,
    string SeatClass,
    decimal Amount,
    DateTime CreatedAt) : IntegrationEvent;

public record PaymentSuccessEvent(
    int PaymentId,
    int BookingId,
    int UserId,
    decimal Amount,
    DateTime ProcessedAt) : IntegrationEvent;

public record PaymentFailedEvent(
    int PaymentId,
    int BookingId,
    int UserId,
    string Reason,
    DateTime FailedAt) : IntegrationEvent;

public record FlightDelayedEvent(
    int FlightId,
    string FlightNumber,
    DateTime NewDepartureTime,
    DateTime NotifiedAt) : IntegrationEvent;

public record BookingCancelledEvent(
    int BookingId,
    int UserId,
    int FlightId,
    int? ScheduleId,
    string SeatClass,
    int TicketCount,
    decimal RefundAmount,
    DateTime CancelledAt) : IntegrationEvent;

public record RewardEarnedEvent(
    int UserId,
    int Points,
    int BookingId,
    DateTime EarnedAt) : IntegrationEvent;

public record CheckInCompletedEvent(
    int BookingId,
    int UserId,
    int FlightId,
    string BoardingPass,
    DateTime CheckedInAt) : IntegrationEvent;

public record BaggageCheckedEvent(
    int BaggageId,
    int BookingId,
    decimal Weight,
    DateTime CheckedAt) : IntegrationEvent;

public record PasswordResetRequestedEvent(
    int UserId,
    string Email,
    string ResetToken,
    DateTime RequestedAt) : IntegrationEvent;

public record UserRegistrationRequestedEvent(
    int UserId,
    string Email,
    string VerificationToken,
    DateTime RequestedAt) : IntegrationEvent;

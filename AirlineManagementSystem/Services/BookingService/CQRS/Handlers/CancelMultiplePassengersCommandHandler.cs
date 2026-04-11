using BookingService.CQRS.Commands;
using BookingService.Repositories;
using BookingService.Services;

namespace BookingService.CQRS.Handlers;

public class CancelMultiplePassengersCommandHandler
{
    private readonly IPassengerRepository _passengerRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IRefundService _refundService;
    private readonly ILogger<CancelMultiplePassengersCommandHandler> _logger;

    public CancelMultiplePassengersCommandHandler(
        IPassengerRepository passengerRepository,
        IBookingRepository bookingRepository,
        IRefundService refundService,
        ILogger<CancelMultiplePassengersCommandHandler> logger)
    {
        _passengerRepository = passengerRepository;
        _bookingRepository = bookingRepository;
        _refundService = refundService;
        _logger = logger;
    }

    public async Task HandleAsync(CancelMultiplePassengersCommand command)
    {
        var booking = await _bookingRepository.GetByIdAsync(command.BookingId);
        if (booking == null)
        {
            throw new InvalidOperationException($"Booking with ID {command.BookingId} not found");
        }

        var passengers = await _passengerRepository.GetPassengersByBookingIdAsync(command.BookingId);
        var passengersToCancel = passengers.Where(p => command.Dto.PassengerIds.Contains(p.Id)).ToList();

        if (!passengersToCancel.Any())
        {
            throw new InvalidOperationException("No valid passengers found to cancel");
        }

        int processedCount = 0;

        foreach (var passenger in passengersToCancel)
        {
            if (passenger.Status == Shared.Models.PassengerStatus.Cancelled || passenger.Status == Shared.Models.PassengerStatus.Refunded)
                continue;

            passenger.Status = Shared.Models.PassengerStatus.Cancelled;
            passenger.CancelledAt = DateTime.UtcNow;
            passenger.CancellationReason = command.Dto.CancellationReason;
            await _passengerRepository.UpdatePassengerAsync(passenger);
            processedCount++;
        }

        if (processedCount > 0)
        {
            booking.CancelledPassengers += processedCount;
            booking.ConfirmedPassengers -= processedCount;

            if (booking.ConfirmedPassengers <= 0)
            {
                booking.Status = Shared.Models.BookingStatus.Cancelled;
            }
            else
            {
                booking.Status = Shared.Models.BookingStatus.PartiallyCancelled;
            }

            booking.UpdatedAt = DateTime.UtcNow;
            await _bookingRepository.UpdateAsync(booking);

            // Process aggregate refund
            await _refundService.ProcessRefundAsync(booking, null, processedCount);

            _logger.LogInformation($"{processedCount} passengers cancelled for Booking {command.BookingId}. Reason: {command.Dto.CancellationReason}");
        }
    }
}

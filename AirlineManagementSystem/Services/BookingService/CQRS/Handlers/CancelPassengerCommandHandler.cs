using BookingService.CQRS.Commands;
using BookingService.DTOs;
using BookingService.Repositories;

namespace BookingService.CQRS.Handlers;

public class CancelPassengerCommandHandler
{
    private readonly IPassengerRepository _passengerRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<CancelPassengerCommandHandler> _logger;

    public CancelPassengerCommandHandler(
        IPassengerRepository passengerRepository,
        IBookingRepository bookingRepository,
        ILogger<CancelPassengerCommandHandler> logger)
    {
        _passengerRepository = passengerRepository;
        _bookingRepository = bookingRepository;
        _logger = logger;
    }

    public async Task HandleAsync(CancelPassengerCommand command)
    {
        var passenger = await _passengerRepository.GetPassengerByIdAsync(command.PassengerId);

        if (passenger == null)
        {
            throw new InvalidOperationException($"Passenger with ID {command.PassengerId} not found");
        }

        if (passenger.Status == BookingService.Models.PassengerStatus.Cancelled)
        {
            throw new InvalidOperationException("Passenger is already cancelled");
        }

        passenger.Status = BookingService.Models.PassengerStatus.Cancelled;
        passenger.CancelledAt = DateTime.UtcNow;
        passenger.CancellationReason = command.Dto.CancellationReason;

        await _passengerRepository.UpdatePassengerAsync(passenger);

        // Update booking passenger count
        var booking = await _bookingRepository.GetByIdAsync(passenger.BookingId);
        if (booking != null)
        {
            booking.CancelledPassengers++;
            booking.ConfirmedPassengers--;
            booking.UpdatedAt = DateTime.UtcNow;
            await _bookingRepository.UpdateAsync(booking);
        }

        _logger.LogInformation($"Passenger {command.PassengerId} cancelled. Reason: {command.Dto.CancellationReason}");
    }
}

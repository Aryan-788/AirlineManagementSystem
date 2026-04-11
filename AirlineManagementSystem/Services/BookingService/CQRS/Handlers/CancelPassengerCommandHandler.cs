using BookingService.CQRS.Commands;
using BookingService.DTOs;
using BookingService.Repositories;
using BookingService.Services;

namespace BookingService.CQRS.Handlers;

public class CancelPassengerCommandHandler
{
    private readonly IPassengerRepository _passengerRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IRefundService _refundService;
    private readonly ILogger<CancelPassengerCommandHandler> _logger;

    public CancelPassengerCommandHandler(
        IPassengerRepository passengerRepository,
        IBookingRepository bookingRepository,
        IRefundService refundService,
        ILogger<CancelPassengerCommandHandler> logger)
    {
        _passengerRepository = passengerRepository;
        _bookingRepository = bookingRepository;
        _refundService = refundService;
        _logger = logger;
    }

    public async Task HandleAsync(CancelPassengerCommand command)
    {
        var passenger = await _passengerRepository.GetPassengerByIdAsync(command.PassengerId);

        if (passenger == null)
        {
            throw new InvalidOperationException($"Passenger with ID {command.PassengerId} not found");
        }

        if (passenger.Status == Shared.Models.PassengerStatus.Cancelled)
        {
            throw new InvalidOperationException("Passenger is already cancelled");
        }

        passenger.Status = Shared.Models.PassengerStatus.Cancelled;
        passenger.CancelledAt = DateTime.UtcNow;
        passenger.CancellationReason = command.Dto.CancellationReason;

        await _passengerRepository.UpdatePassengerAsync(passenger);

        // Update booking passenger count
        var booking = await _bookingRepository.GetByIdAsync(passenger.BookingId);
        if (booking != null)
        {
            booking.CancelledPassengers++;
            booking.ConfirmedPassengers--;
            
            // Check if all passengers are now cancelled to update parent booking status
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

            // Process refund for the single passenger
            await _refundService.ProcessRefundAsync(booking, passenger.Id, 1);
        }

        _logger.LogInformation($"Passenger {command.PassengerId} cancelled. Reason: {command.Dto.CancellationReason}");
    }
}

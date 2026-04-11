using BookingService.CQRS.Commands;
using BookingService.DTOs;
using BookingService.Models;
using BookingService.Repositories;

namespace BookingService.CQRS.Handlers;

public class CreatePassengerCommandHandler
{
    private readonly IPassengerRepository _passengerRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<CreatePassengerCommandHandler> _logger;

    public CreatePassengerCommandHandler(
        IPassengerRepository passengerRepository,
        IBookingRepository bookingRepository,
        ILogger<CreatePassengerCommandHandler> logger)
    {
        _passengerRepository = passengerRepository;
        _bookingRepository = bookingRepository;
        _logger = logger;
    }

    public async Task<PassengerResponseDto> HandleAsync(CreatePassengerCommand command)
    {
        var dto = command.Dto;

        // Validate booking exists
        var booking = await _bookingRepository.GetByIdAsync(command.BookingId);
        if (booking == null)
        {
            throw new InvalidOperationException($"Booking with ID {command.BookingId} not found");
        }

        // Validate Aadhar uniqueness within the same schedule
        bool isDuplicate = false;
        if (booking.ScheduleId.HasValue)
        {
            isDuplicate = await _passengerRepository.IsAadharDuplicateInScheduleAsync(dto.AadharCardNo, booking.ScheduleId.Value);
        }
        else
        {
            // Fallback for safety if schedule ID is missing
            isDuplicate = !await _passengerRepository.IsAadharUniqueAsync(dto.AadharCardNo);
        }

        if (isDuplicate)
        {
            throw new InvalidOperationException("This Aadhar card number is already registered for this flight");
        }

        var passenger = new Passenger
        {
            BookingId = command.BookingId,
            Name = dto.Name,
            Age = dto.Age,
            Gender = dto.Gender,
            AadharCardNo = dto.AadharCardNo,
            SeatNumber = dto.SeatNumber,
            Status = Shared.Models.PassengerStatus.Confirmed
        };

        await _passengerRepository.AddPassengerAsync(passenger);

        // Update booking passenger count
        booking.TotalPassengers++;
        booking.ConfirmedPassengers++;
        booking.UpdatedAt = DateTime.UtcNow;
        await _bookingRepository.UpdateAsync(booking);

        _logger.LogInformation($"Passenger {passenger.Id} created for booking {command.BookingId}");

        return MapToResponseDto(passenger);
    }

    private async Task<bool> ValidateAadharNumberAsync(string aadharCardNo)
    {
        // Validate 12 digit format
        if (string.IsNullOrEmpty(aadharCardNo) || !System.Text.RegularExpressions.Regex.IsMatch(aadharCardNo, @"^\d{12}$"))
        {
            return false;
        }

        // Check uniqueness in database
        return await _passengerRepository.IsAadharUniqueAsync(aadharCardNo);
    }

    private PassengerResponseDto MapToResponseDto(Passenger passenger)
    {
        return new PassengerResponseDto
        {
            Id = passenger.Id,
            Name = passenger.Name,
            Age = passenger.Age,
            Gender = passenger.Gender,
            AadharCardNo = passenger.AadharCardNo,
            Status = passenger.Status.ToString(),
            SeatNumber = passenger.SeatNumber,
            CancelledAt = passenger.CancelledAt,
            CancellationReason = passenger.CancellationReason,
            CreatedAt = passenger.CreatedAt
        };
    }
}

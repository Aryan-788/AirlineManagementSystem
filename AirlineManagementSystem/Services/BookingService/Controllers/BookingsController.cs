using BookingService.CQRS.Commands;
using BookingService.CQRS.Handlers;
using BookingService.CQRS.Queries;
using BookingService.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly CreateBookingCommandHandler _createBookingHandler;
    private readonly CancelBookingCommandHandler _cancelBookingHandler;
    private readonly CreatePassengerCommandHandler _createPassengerHandler;
    private readonly CancelPassengerCommandHandler _cancelPassengerHandler;
    private readonly GetBookingByIdQueryHandler _getBookingHandler;
    private readonly GetBookingHistoryQueryHandler _getBookingHistoryHandler;
    private readonly GetBookingsByScheduleQueryHandler _getBookingsByScheduleHandler;
    private readonly GetOccupiedSeatsQueryHandler _getOccupiedSeatsHandler;
    private readonly GetPassengersForBookingQueryHandler _getPassengersHandler;
    private readonly GetBookingByPnrQueryHandler _getBookingByPnrHandler;
    private readonly ILogger<BookingsController> _logger;

    public BookingsController(
        CreateBookingCommandHandler createBookingHandler,
        CancelBookingCommandHandler cancelBookingHandler,
        CreatePassengerCommandHandler createPassengerHandler,
        CancelPassengerCommandHandler cancelPassengerHandler,
        GetBookingByIdQueryHandler getBookingHandler,
        GetBookingHistoryQueryHandler getBookingHistoryHandler,
        GetBookingsByScheduleQueryHandler getBookingsByScheduleHandler,
        GetOccupiedSeatsQueryHandler getOccupiedSeatsHandler,
        GetPassengersForBookingQueryHandler getPassengersHandler,
        GetBookingByPnrQueryHandler getBookingByPnrHandler,
        ILogger<BookingsController> logger)
    {
        _createBookingHandler = createBookingHandler;
        _cancelBookingHandler = cancelBookingHandler;
        _createPassengerHandler = createPassengerHandler;
        _cancelPassengerHandler = cancelPassengerHandler;
        _getBookingHandler = getBookingHandler;
        _getBookingHistoryHandler = getBookingHistoryHandler;
        _getBookingsByScheduleHandler = getBookingsByScheduleHandler;
        _getOccupiedSeatsHandler = getOccupiedSeatsHandler;
        _getPassengersHandler = getPassengersHandler;
        _getBookingByPnrHandler = getBookingByPnrHandler;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new booking.
    /// </summary>
    /// <param name="dto">The booking details.</param>
    /// <returns>The created booking.</returns>
    [HttpPost]
    [Authorize(Roles = "Passenger,Dealer")]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto dto)
    {
        try
        {
            var command = new CreateBookingCommand(dto);
            var result = await _createBookingHandler.HandleAsync(command);
            return CreatedAtAction(nameof(GetBooking), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating booking: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Adds passengers to an existing booking.
    /// </summary>
    /// <param name="bookingId">The ID of the booking.</param>
    /// <param name="passengers">The list of passengers to add.</param>
    /// <returns>The added passengers.</returns>
    [HttpPost("{bookingId}/passengers")]
    [Authorize(Roles = "Passenger,Dealer")]
    public async Task<IActionResult> AddPassengersToBooking(int bookingId, [FromBody] List<CreatePassengerDto> passengers)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (passengers == null || passengers.Count == 0)
                return BadRequest(new { message = "At least one passenger is required" });

            var addedPassengers = new List<PassengerResponseDto>();

            foreach (var passengerDto in passengers)
            {
                var command = new CreatePassengerCommand(bookingId, passengerDto);
                var passenger = await _createPassengerHandler.HandleAsync(command);
                addedPassengers.Add(passenger);
            }

            return CreatedAtAction(nameof(GetBookingPassengers), new { bookingId = bookingId }, addedPassengers);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning($"Invalid operation: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error adding passengers: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }


    /// <summary>
    /// Gets all passengers for a specific booking.
    /// </summary>
    /// <param name="bookingId">The ID of the booking.</param>
    /// <returns>The list of passengers.</returns>
    [HttpGet("{bookingId}/passengers")]
    [Authorize(Roles = "Passenger,Dealer")]
    public async Task<IActionResult> GetBookingPassengers(int bookingId)
    {
        try
        {
            var query = new GetPassengersForBookingQuery(bookingId);
            var passengers = await _getPassengersHandler.HandleAsync(query);
            return Ok(passengers);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting passengers: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Cancels a specific passenger from a booking.
    /// </summary>
    /// <param name="passengerId">The ID of the passenger to cancel.</param>
    /// <param name="dto">The cancellation details.</param>
    /// <returns>A success message.</returns>
    [HttpPost("passengers/{passengerId}/cancel")]
    [Authorize(Roles = "Passenger,Dealer")]
    public async Task<IActionResult> CancelPassenger(int passengerId, [FromBody] CancelPassengerDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var command = new CancelPassengerCommand(passengerId, dto);
            await _cancelPassengerHandler.HandleAsync(command);
            return Ok(new { message = "Passenger cancelled successfully" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning($"Invalid operation: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error cancelling passenger: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }


    /// <summary>
    /// Gets a booking by its ID.
    /// </summary>
    /// <param name="id">The ID of the booking.</param>
    /// <returns>The booking details.</returns>
    [HttpGet("{id}")]
    [Authorize(Roles = "Passenger,Dealer")]
    public async Task<IActionResult> GetBooking(int id)
    {
        try
        {
            var query = new GetBookingByIdQuery(id);
            var result = await _getBookingHandler.HandleAsync(query);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gets a booking by its PNR.
    /// </summary>
    /// <param name="pnr">The PNR of the booking.</param>
    /// <returns>The booking details.</returns>
    [HttpGet("pnr/{pnr}")]
    [Authorize(Roles = "Passenger,Dealer,Admin,GroundStaff")]
    public async Task<IActionResult> GetBookingByPnr(string pnr)
    {
        try
        {
            var query = new GetBookingByPnrQuery(pnr);
            var result = await _getBookingByPnrHandler.HandleAsync(query);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cancels a booking.
    /// </summary>
    /// <param name="id">The ID of the booking to cancel.</param>
    /// <returns>A success message if the booking is cancelled successfully.</returns>
    [HttpPost("{id}/cancel")]
    [Authorize(Roles = "Passenger,Dealer")]
    public async Task<IActionResult> CancelBooking(int id)
    {
        try
        {
            var command = new CancelBookingCommand(id);
            await _cancelBookingHandler.HandleAsync(command);
            return Ok(new { message = "Booking cancelled successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gets the booking history for a user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>The booking history for the user.</returns>
    [HttpGet("history/{userId}")]
    [Authorize(Roles = "Passenger,Dealer")]
    public async Task<IActionResult> GetBookingHistory(int userId)
    {
        var query = new GetBookingHistoryQuery(userId);
        var results = await _getBookingHistoryHandler.HandleAsync(query);
        return Ok(results);
    }

    /// <summary>
    /// Gets all bookings for a specific flight schedule.
    /// </summary>
    /// <param name="scheduleId">The ID of the flight schedule.</param>
    /// <returns>The list of bookings for the schedule.</returns>
    [HttpGet("schedule/{scheduleId}")]
    [Authorize(Roles = "Admin,Dealer")]
    public async Task<IActionResult> GetBookingsBySchedule(int scheduleId)
    {
        try
        {
            var query = new GetBookingsByScheduleQuery(scheduleId);
            var results = await _getBookingsByScheduleHandler.HandleAsync(query);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting schedule bookings: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }


    /// <summary>
    /// Gets the occupied seats for a flight schedule.
    /// </summary>
    /// <param name="flightId">The ID of the flight.</param>
    /// <param name="scheduleId">The ID of the flight schedule.</param>
    /// <returns>The list of occupied seats for the schedule.</returns>
    [HttpGet("occupied-seats")]
    [Authorize(Roles = "Passenger,Dealer,Admin")]
    public async Task<IActionResult> GetOccupiedSeats([FromQuery] int flightId, [FromQuery] int? scheduleId)
    {
        try
        {
            var query = new GetOccupiedSeatsQuery(flightId, scheduleId);
            var seats = await _getOccupiedSeatsHandler.HandleAsync(query);
            return Ok(seats);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting occupied seats: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

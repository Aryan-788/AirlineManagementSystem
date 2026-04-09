using FlightService.DTOs;
using FlightService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlightService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FlightsController : ControllerBase
{
    private readonly IFlightService _flightService;
    private readonly IFlightScheduleService _scheduleService;

    public FlightsController(IFlightService flightService, IFlightScheduleService scheduleService)
    {
        _flightService = flightService;
        _scheduleService = scheduleService;
    }

    // ─── Flight Endpoints ───
    /// <summary>
    /// Create Flight
    /// </summary>
    /// <param name="dto"></param>
    /// <returns>returns flight details</returns>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateFlight([FromBody] CreateFlightDto dto)
    {
        try
        {
            var result = await _flightService.CreateFlightAsync(dto);
            return CreatedAtAction(nameof(GetFlight), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    /// <summary>
    /// Get Flight by ID
    /// </summary>
    /// <param name="id"></param>
    /// <returns>returns flight details</returns>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetFlight(int id)
    {
        try
        {
            var result = await _flightService.GetFlightAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update Flight
    /// </summary>
    /// <param name="id"></param>
    /// <param name="dto"></param>
    /// <returns>returns flight details</returns>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateFlight(int id, [FromBody] UpdateFlightDto dto)
    {
        try
        {
            var result = await _flightService.UpdateFlightAsync(id, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete Flight
    /// </summary>
    /// <param name="id"></param>
    /// <returns>returns no content</returns>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteFlight(int id)
    {
        try
        {
            await _flightService.DeleteFlightAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Search Flights
    /// </summary>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    /// <param name="departureDate"></param>
    /// <returns>returns list of flights</returns>
    [HttpGet("search")]
    public async Task<IActionResult> SearchFlights([FromQuery] string source, [FromQuery] string destination, [FromQuery] DateTime departureDate)
    {
        var results = await _flightService.SearchFlightsAsync(source, destination, departureDate);
        return Ok(results);
    }

    /// <summary>
    /// Get All Flights
    /// </summary>
    /// <returns>returns list of flights</returns>
    [HttpGet]
    public async Task<IActionResult> GetAllFlights()
    {
        var results = await _flightService.GetAllFlightsAsync();
        return Ok(results);
    }

    /// <summary>
    /// Delay Flight
    /// </summary>
    /// <param name="id"></param>
    /// <param name="newDepartureTime"></param>
    /// <returns>returns message</returns>
    [HttpPost("{id:int}/delay")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DelayFlight(int id, [FromBody] DateTime newDepartureTime)
    {
        try
        {
            await _flightService.DelayFlightAsync(id, newDepartureTime);
            return Ok(new { message = "Flight delayed successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cancel Flight
    /// </summary>
    /// <param name="id"></param>
    /// <returns>returns message</returns>
    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CancelFlight(int id)
    {
        try
        {
            await _flightService.CancelFlightAsync(id);
            return Ok(new { message = "Flight cancelled successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Assign Gate
    /// </summary>
    /// <param name="id"></param>
    /// <param name="gate"></param>
    /// <returns>returns message</returns>
    [HttpPost("{id:int}/assign-gate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignGate(int id, [FromBody] string gate)
    {
        try
        {
            await _flightService.AssignGateAsync(id, gate);
            return Ok(new { message = "Gate assigned successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Assign Aircraft
    /// </summary>
    /// <param name="id"></param>
    /// <param name="aircraft"></param>
    /// <returns>returns message</returns>
    [HttpPost("{id:int}/assign-aircraft")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignAircraft(int id, [FromBody] string aircraft)
    {
        try
        {
            await _flightService.AssignAircraftAsync(id, aircraft);
            return Ok(new { message = "Aircraft assigned successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Assign Crew
    /// </summary>
    /// <param name="id"></param>
    /// <param name="crew"></param>
    /// <returns>returns message</returns>
    [HttpPost("{id:int}/assign-crew")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignCrew(int id, [FromBody] string crew)
    {
        try
        {
            await _flightService.AssignCrewAsync(id, crew);
            return Ok(new { message = "Crew assigned successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Book Seat
    /// </summary>
    /// <param name="id"></param>
    /// <param name="dto"></param>
    /// <returns>returns message</returns>
    [HttpPost("{id:int}/book-seat")]
    public async Task<IActionResult> BookSeat(int id, [FromBody] BookSeatDto dto)
    {
        try
        {
            await _flightService.BookSeatAsync(id, dto.SeatClass, dto.Count);
            return Ok(new { message = "Seat booked successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ─── Schedule Endpoints ───

    /// <summary>
    /// Create Schedule
    /// </summary>
    /// <param name="dto"></param>
    /// <returns>returns schedule details</returns>
    [HttpPost("schedules")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateSchedule([FromBody] CreateScheduleDto dto)
    {
        try
        {
            var result = await _scheduleService.CreateScheduleAsync(dto);
            return CreatedAtAction(nameof(GetSchedule), new { id = result.Id }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get All Schedules
    /// </summary>
    /// <returns>returns list of schedules</returns>
    [HttpGet("schedules")]
    public async Task<IActionResult> GetAllSchedules()
    {
        var results = await _scheduleService.GetAllSchedulesAsync();
        return Ok(results);
    }

    /// <summary>
    /// Get Schedule by ID
    /// </summary>
    /// <param name="id"></param>
    /// <returns>returns schedule details</returns>
    [HttpGet("schedules/{id:int}")]
    public async Task<IActionResult> GetSchedule(int id)
    {
        try
        {
            var result = await _scheduleService.GetScheduleAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Search Schedules
    /// </summary>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    /// <param name="departureDate"></param>
    /// <returns>returns list of schedules</returns>
    [HttpGet("schedules/search")]
    public async Task<IActionResult> SearchSchedules([FromQuery] string source, [FromQuery] string destination, [FromQuery] DateTime departureDate)
    {
        var results = await _scheduleService.SearchSchedulesAsync(source, destination, departureDate);
        return Ok(results);
    }

    /// <summary>
    /// Get Schedules by Flight ID
    /// </summary>
    /// <param name="flightId"></param>
    /// <returns>returns list of schedules</returns>
    [HttpGet("{flightId:int}/schedules")]
    public async Task<IActionResult> GetSchedulesByFlight(int flightId)
    {
        var results = await _scheduleService.GetSchedulesByFlightIdAsync(flightId);
        return Ok(results);
    }

    /// <summary>
    /// Update Schedule
    /// </summary>
    /// <param name="id"></param>
    /// <param name="dto"></param>
    /// <returns>returns updated schedule</returns>
    [HttpPut("schedules/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateSchedule(int id, [FromBody] UpdateScheduleDto dto)
    {
        try
        {
            var result = await _scheduleService.UpdateScheduleAsync(id, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
    /// <summary>
    /// Delete Schedule
    /// </summary>
    /// <param name="id"></param>
    /// <returns>returns no content</returns>
    [HttpDelete("schedules/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteSchedule(int id)
    {
        try
        {
            await _scheduleService.DeleteScheduleAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cancel Schedule
    /// </summary>
    /// <param name="id"></param>
    /// <returns>returns message</returns>
    [HttpPost("schedules/{id:int}/cancel")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CancelSchedule(int id)
    {
        try
        {
            await _scheduleService.CancelScheduleAsync(id);
            return Ok(new { message = "Schedule cancelled successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
    /// <summary>
    /// Book Seat on Schedule
    /// </summary>
    /// <param name="id"></param>
    /// <param name="dto"></param>
    /// <returns>returns message</returns>
    [HttpPost("schedules/{id:int}/book-seat")]
    public async Task<IActionResult> BookScheduleSeat(int id, [FromBody] BookSeatDto dto)
    {
        try
        {
            await _scheduleService.BookScheduleSeatAsync(id, dto.SeatClass, dto.Count);
            return Ok(new { message = "Seat booked on schedule successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public class BookSeatDto
{
    public string SeatClass { get; set; } = "";
    public int Count { get; set; } = 1;
}

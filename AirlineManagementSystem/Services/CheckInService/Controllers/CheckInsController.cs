using CheckInService.DTOs;
using CheckInService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CheckInService.Controllers;
/// <summary>
/// CheckIns Controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CheckInsController : ControllerBase
{
    private readonly ICheckInService _checkInService;

    public CheckInsController(ICheckInService checkInService)
    {
        _checkInService = checkInService;
    }

    /// <summary>
    /// Online Check-In
    /// </summary>
    /// <param name="dto"></param>
    /// <param name="passengerName"></param>
    /// <param name="flightNumber"></param>
    /// <param name="flightId"></param>
    /// <param name="departureTime"></param>
    /// <returns>returns check-in details</returns>
    [HttpPost("online")]
    [Authorize(Roles = "Passenger")]
    public async Task<IActionResult> OnlineCheckIn(
        [FromBody] OnlineCheckInDto dto,
        [FromQuery] string passengerName,
        [FromQuery] string flightNumber,
        [FromQuery] int flightId,
        [FromQuery] DateTime departureTime)
    {
        try
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var result = await _checkInService.OnlineCheckInAsync(dto, passengerName, flightNumber, flightId, departureTime, token);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get Check-In by ID
    /// </summary>
    /// <param name="id"></param>
    /// <returns>returns check-in details</returns>
    [HttpGet("{id}")]
    [Authorize(Roles = "Passenger")]
    public async Task<IActionResult> GetCheckIn(int id)
    {
        try
        {
            var result = await _checkInService.GetCheckInAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
    /// <summary>
    /// Generate Boarding Pass
    /// </summary>
    /// <param name="id"></param>
    /// <returns>returns boarding pass</returns>
    [HttpGet("{id}/boarding-pass")]
    [Authorize(Roles = "Passenger")]
    public async Task<IActionResult> GenerateBoardingPass(int id)
    {
        try
        {
            var result = await _checkInService.GenerateBoardingPassAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
    /// <summary>
    /// Get Check-In Summary
    /// </summary>
    /// <returns>returns check-in summary</returns>
    [HttpGet("summary")]
    [Authorize(Roles = "GroundStaff,Admin")]
    public async Task<IActionResult> GetSummary()
    {
        try
        {
            var result = await _checkInService.GetSummaryAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

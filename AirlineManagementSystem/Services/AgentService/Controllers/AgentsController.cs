using AgentService.DTOs;
using AgentService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgentService.Controllers;

/// <summary>
/// Agents Controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AgentsController : ControllerBase
{
    private readonly IAgentService _agentService;

    public AgentsController(IAgentService agentService)
    {
        _agentService = agentService;
    }

    /// <summary>
    /// Create Dealer
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost("dealer")]
    [Authorize(Roles = "Admin,Dealer")]
    public async Task<IActionResult> CreateDealer([FromBody] CreateDealerDto dto)
    {
        try
        {
            var result = await _agentService.CreateDealerAsync(dto);
            return CreatedAtAction(nameof(GetDealer), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get Dealer
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("dealer/{id}")]
    [Authorize]
    public async Task<IActionResult> GetDealer(int id)
    {
        try
        {
            var result = await _agentService.GetDealerAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get Dealer By Email
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    [HttpGet("dealer/by-email")]
    [Authorize(Roles = "Dealer,Admin")]
    public async Task<IActionResult> GetDealerByEmail([FromQuery] string email)
    {
        try
        {
            var result = await _agentService.GetDealerByEmailAsync(email);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Allocate Seats
    /// </summary>
    /// <param name="dealerId"></param>
    /// <param name="seats"></param>
    /// <returns></returns>
    [HttpPost("dealer/{dealerId}/allocate-seats")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AllocateSeats(int dealerId, [FromQuery] int seats)
    {
        try
        {
            var result = await _agentService.AllocateSeatsAsync(dealerId, seats);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Record Dealer Booking
    /// </summary>
    /// <param name="dealerId"></param>
    /// <param name="bookingId"></param>
    /// <param name="flightId"></param>
    /// <param name="bookingAmount"></param>
    /// <returns></returns>
    [HttpPost("booking/record")]
    [Authorize(Roles = "Dealer,Admin")]
    public async Task<IActionResult> RecordDealerBooking(
        [FromQuery] int dealerId,
        [FromQuery] int bookingId,
        [FromQuery] int flightId,
        [FromQuery] decimal bookingAmount)
    {
        try
        {
            var result = await _agentService.RecordDealerBookingAsync(dealerId, bookingId, flightId, bookingAmount);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get Commission Report
    /// </summary>
    /// <returns></returns>
    [HttpGet("commission-report")]
    [Authorize(Roles = "Dealer,Admin")]
    public async Task<IActionResult> GetCommissionReport()
    {
        try
        {
            var result = await _agentService.GetCommissionReportAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get All Dealers
    /// </summary>
    /// <returns></returns>
    [HttpGet("dealers")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllDealers()
    {
        try
        {
            var result = await _agentService.GetAllDealersAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get Dealer Performance
    /// </summary>
    /// <param name="dealerId"></param>
    /// <returns></returns>
    [HttpGet("dealer/{dealerId}/performance")]
    [Authorize(Roles = "Dealer,Admin")]
    public async Task<IActionResult> GetDealerPerformance(int dealerId)
    {
        try
        {
            var result = await _agentService.GetDealerPerformanceAsync(dealerId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

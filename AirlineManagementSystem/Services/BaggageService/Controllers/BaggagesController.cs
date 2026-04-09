using BaggageService.DTOs;
using BaggageService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BaggageService.Controllers;

/// <summary>
/// Baggage Controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BaggagesController : ControllerBase
{
    private readonly IBaggageService _baggageService;

    public BaggagesController(IBaggageService baggageService)
    {
        _baggageService = baggageService;
    }

    /// <summary>
    /// Add Baggage
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost]
    [Authorize(Roles = "GroundStaff")]
    public async Task<IActionResult> AddBaggage([FromBody] AddBaggageDto dto)
    {
        try
        {
            var result = await _baggageService.AddBaggageAsync(dto);
            return CreatedAtAction(nameof(GetBaggage), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    /// <summary>
    /// Get Baggage by ID
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id}")]
    [Authorize(Roles = "GroundStaff,Passenger,Dealer")]
    public async Task<IActionResult> GetBaggage(int id)
    {
        try
        {
            var result = await _baggageService.GetBaggageAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update Baggage Status
    /// </summary>
    /// <param name="id"></param>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPut("{id}/status")]
    [Authorize(Roles = "GroundStaff")]
    public async Task<IActionResult> UpdateBaggageStatus(int id, [FromBody] UpdateBaggageStatusDto dto)
    {
        try
        {
            var result = await _baggageService.UpdateBaggageStatusAsync(id, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Mark Baggage as Delivered
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpPost("{id}/deliver")]
    [Authorize(Roles = "GroundStaff")]
    public async Task<IActionResult> MarkDelivered(int id)
    {
        try
        {
            var result = await _baggageService.MarkDeliveredAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get Baggage by Booking ID
    /// </summary>
    /// <param name="bookingId"></param>
    /// <returns></returns>
    [HttpGet("booking/{bookingId}")]
    [Authorize(Roles = "GroundStaff,Passenger,Dealer")]
    public async Task<IActionResult> GetByBookingId(int bookingId)
    {
        var results = await _baggageService.GetByBookingIdAsync(bookingId);
        return Ok(results);
    }

    /// <summary>
    /// Track Baggage
    /// </summary>
    /// <param name="trackingNumber"></param>
    /// <returns></returns>
    [HttpGet("track/{trackingNumber}")]
    [Authorize(Roles = "Passenger,GroundStaff,Dealer")]
    public async Task<IActionResult> TrackBaggage(string trackingNumber)
    {
        try
        {
            var result = await _baggageService.TrackBaggageAsync(trackingNumber);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get All Baggage
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Authorize(Roles = "GroundStaff")]
    public async Task<IActionResult> GetAll()
    {
        var results = await _baggageService.GetAllBaggageAsync();
        return Ok(results);
    }

    /// <summary>
    /// Get Baggage Summary
    /// </summary>
    /// <returns></returns>
    [HttpGet("summary")]
    [Authorize(Roles = "GroundStaff,Admin")]
    public async Task<IActionResult> GetSummary()
    {
        try
        {
            var result = await _baggageService.GetSummaryAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

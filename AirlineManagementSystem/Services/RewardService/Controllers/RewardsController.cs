using RewardService.DTOs;
using RewardService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RewardService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RewardsController : ControllerBase
{
    private readonly IRewardService _rewardService;

    public RewardsController(IRewardService rewardService)
    {
        _rewardService = rewardService;
    }

    /// <summary>
    /// Earn Points
    /// </summary>
    /// <param name="dto"></param>
    /// <param name="transactionType"></param>
    /// <param name="bookingId"></param>
    /// <returns>returns reward details</returns>
    [HttpPost("earn")]
    [Authorize(Roles = "Passenger,Dealer")]
    public async Task<IActionResult> EarnPoints(
        [FromBody] RewardDto dto,
        [FromQuery] string transactionType,
        [FromQuery] int? bookingId)
    {
        try
        {
            var result = await _rewardService.EarnPointsAsync(dto.UserId, dto.Points, transactionType, bookingId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get Balance
    /// </summary>
    /// <param name="userId"></param>
    /// <returns>returns balance details</returns>
    [HttpGet("{userId}/balance")]
    [Authorize(Roles = "Passenger")]
    public async Task<IActionResult> GetBalance(int userId)
    {
        try
        {
            var result = await _rewardService.GetBalanceAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get History
    /// </summary>
    /// <param name="userId"></param>
    /// <returns>returns history details</returns>
    [HttpGet("{userId}/history")]
    [Authorize(Roles = "Passenger")]
    public async Task<IActionResult> GetHistory(int userId)
    {
        try
        {
            var result = await _rewardService.GetHistoryAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Redeem Points
    /// </summary>
    /// <param name="dto"></param>
    /// <returns>returns reward details</returns>
    [HttpPost("redeem")]
    [Authorize(Roles = "Passenger")]
    public async Task<IActionResult> RedeemPoints([FromBody] RedeemRewardDto dto)
    {
        try
        {
            var result = await _rewardService.RedeemPointsAsync(dto.UserId, dto.Points);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

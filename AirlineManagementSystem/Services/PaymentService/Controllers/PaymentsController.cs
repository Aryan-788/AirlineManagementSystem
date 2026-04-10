using PaymentService.DTOs;
using PaymentService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PaymentService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    /// <summary>
    /// Create Order
    /// </summary>
    /// <param name="dto"></param>
    /// <returns>returns order details</returns>
    [HttpPost("create-order")]
    [Authorize(Roles = "Passenger,Dealer")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        try
        {
            var result = await _paymentService.CreateOrderAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Verify Payment
    /// </summary>
    /// <param name="dto"></param>
    /// <returns>returns payment details</returns>
    [HttpPost("verify")]
    [Authorize(Roles = "Passenger,Dealer")]
    public async Task<IActionResult> VerifyPayment([FromBody] VerifySignatureDto dto)
    {
        try
        {
            var result = await _paymentService.VerifySignatureAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Process Payment
    /// </summary>
    /// <param name="dto"></param>
    /// <returns>returns payment details</returns>
    [HttpPost("process")]
    [Authorize(Roles = "Passenger,Dealer")]
    public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentDto dto)
    {
        try
        {
            var result = await _paymentService.ProcessPaymentAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get Payment by ID
    /// </summary>
    /// <param name="id"></param>
    /// <returns>returns payment details</returns>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetPayment(int id)
    {
        try
        {
            var result = await _paymentService.GetPaymentAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Refund Payment
    /// </summary>
    /// <param name="id"></param>
    /// <returns>returns payment details</returns>
    [HttpPost("{id}/refund")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Refund(int id)
    {
        try
        {
            var result = await _paymentService.RefundAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get All Payments (Admin only)
    /// </summary>
    /// <returns>All payments in the system</returns>
    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllPayments()
    {
        var result = await _paymentService.GetAllPaymentsAsync();
        return Ok(result);
    }
}

using Hotel.Api.DTOs;
using Hotel.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Hotel.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost("midtrans/webhook")]
    public async Task<IActionResult> MidtransWebhook([FromBody] MidtransWebhookDto dto)
    {
        try
        {
            var payment = await _paymentService.HandleMidtransWebhookAsync(dto);

            return Ok(new
            {
                message = "Payment webhook processed",
                paymentId = payment.Id,
                status = payment.Status
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

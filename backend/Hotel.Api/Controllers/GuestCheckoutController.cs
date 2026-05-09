using Hotel.Api.DTOs;
using Hotel.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Hotel.Api.Controllers;

[ApiController]
[Route("api/guest")]
public class GuestCheckoutController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public GuestCheckoutController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpPost("checkout")]
    [EnableRateLimiting("booking")]
    public async Task<IActionResult> Checkout([FromBody] GuestCheckoutDto dto)
    {
        try
        {
            var result = await _bookingService.GuestCheckoutAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

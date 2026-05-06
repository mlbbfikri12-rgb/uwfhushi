using Hotel.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Hotel.Api.Controllers;

[ApiController]
[Route("api/hotel")]
public class HotelPublicController : ControllerBase
{
    private readonly IHotelPublicService _service;

    public HotelPublicController(IHotelPublicService service)
    {
        _service = service;
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetHotel(string slug, CancellationToken ct)
    {
        var result = await _service.GetHotelAsync(slug, ct);
        return Ok(result);
    }

    [HttpGet("{slug}/pricing")]
    public async Task<IActionResult> GetPricing(
        string slug,
        [FromQuery] DateTime checkIn,
        [FromQuery] DateTime checkOut,
        CancellationToken ct)
    {
        var result = await _service.GetPricingAsync(slug, checkIn, checkOut, ct);
        return Ok(result);
    }

    // 🔥 FIXED ENDPOINT
    [HttpGet("{slug}/room/{roomTypeId}")]
    public async Task<IActionResult> GetRoomDetail(
        string slug,
        Guid roomTypeId,
        CancellationToken ct)
    {
        var result = await _service.GetRoomDetailAsync(slug, roomTypeId, ct);
        return Ok(result);
    }
}
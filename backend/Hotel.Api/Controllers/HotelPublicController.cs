using Hotel.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Hotel.Api.Controllers;

[ApiController]
[Route("api/hotel")]
public class HotelPublicController : ControllerBase
{
    private readonly IHotelPublicService _hotelPublicService;

    public HotelPublicController(IHotelPublicService hotelPublicService)
    {
        _hotelPublicService = hotelPublicService;
    }

    [HttpGet("{branch}/full")]
    public async Task<IActionResult> GetHotelFull(
        string branch,
        [FromQuery] DateTime checkIn,
        [FromQuery] DateTime checkOut,
        [FromQuery] int adult = 1,
        [FromQuery] int child = 0,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var hotel = await _hotelPublicService.GetHotelFullAsync(
                branch,
                checkIn,
                checkOut,
                adult,
                child,
                cancellationToken);

            return Ok(hotel);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("slug/{slug}/full")]
    public async Task<IActionResult> GetHotelFullBySlug(
        string slug,
        [FromQuery] DateTime checkIn,
        [FromQuery] DateTime checkOut,
        [FromQuery] int adult = 1,
        [FromQuery] int child = 0,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var hotel = await _hotelPublicService.GetHotelFullBySlugAsync(
                slug,
                checkIn,
                checkOut,
                adult,
                child,
                cancellationToken);

            return Ok(hotel);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

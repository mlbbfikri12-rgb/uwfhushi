using Hotel.Api.DTOs;
using Hotel.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Hotel.Api.Controllers;

[ApiController]
[Route("api/public/hotels")]
public class PublicHotelsController : ControllerBase
{
    private readonly IPublicHotelSearchService _searchService;

    public PublicHotelsController(IPublicHotelSearchService searchService)
    {
        _searchService = searchService;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] PublicHotelSearchQueryDto query, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _searchService.SearchAsync(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

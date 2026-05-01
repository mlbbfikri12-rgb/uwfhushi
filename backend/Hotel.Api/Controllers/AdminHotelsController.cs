using Hotel.Api.DTOs;
using Hotel.Api.Entities.Master;
using Hotel.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hotel.Api.Controllers;

[ApiController]
[Route("api/admin/hotels")]
[Authorize(Roles = StaffRoles.SuperAdmin)]
public class AdminHotelsController : ControllerBase
{
    private readonly IHotelAdminService _hotelAdminService;

    public AdminHotelsController(IHotelAdminService hotelAdminService) => _hotelAdminService = hotelAdminService;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? q, CancellationToken cancellationToken)
    {
        return Ok(await _hotelAdminService.GetAsync(q, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var data = await _hotelAdminService.GetByIdAsync(id, cancellationToken);
        return data == null ? NotFound(new { error = "Hotel not found" }) : Ok(data);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] HotelUpsertDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var data = await _hotelAdminService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = data.Id }, data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] HotelUpsertDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var data = await _hotelAdminService.UpdateAsync(id, dto, cancellationToken);
            return data == null ? NotFound(new { error = "Hotel not found" }) : Ok(data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _hotelAdminService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound(new { error = "Hotel not found" });
    }

    [HttpPost("{id:guid}/images")]
    public async Task<IActionResult> AddImage(Guid id, [FromBody] AddHotelImageDto dto, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _hotelAdminService.AddImageAsync(id, dto, cancellationToken));
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}/images/{imageId:guid}")]
    public async Task<IActionResult> DeleteImage(Guid id, Guid imageId, CancellationToken cancellationToken)
    {
        var deleted = await _hotelAdminService.DeleteImageAsync(id, imageId, cancellationToken);
        return deleted ? NoContent() : NotFound(new { error = "Hotel image not found" });
    }

    [HttpPost("{id:guid}/facilities")]
    public async Task<IActionResult> SetFacilities(Guid id, [FromBody] SetHotelFacilitiesDto dto, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _hotelAdminService.SetFacilitiesAsync(id, dto.FacilityIds, cancellationToken));
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/nearby-places")]
    public async Task<IActionResult> AddNearbyPlace(Guid id, [FromBody] AddNearbyPlaceDto dto, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _hotelAdminService.AddNearbyPlaceAsync(id, dto, cancellationToken));
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}/nearby-places/{nearbyPlaceId:guid}")]
    public async Task<IActionResult> DeleteNearbyPlace(Guid id, Guid nearbyPlaceId, CancellationToken cancellationToken)
    {
        var deleted = await _hotelAdminService.DeleteNearbyPlaceAsync(id, nearbyPlaceId, cancellationToken);
        return deleted ? NoContent() : NotFound(new { error = "Nearby place not found" });
    }
}

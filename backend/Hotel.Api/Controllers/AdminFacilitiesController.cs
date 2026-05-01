using Hotel.Api.DTOs;
using Hotel.Api.Entities.Master;
using Hotel.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hotel.Api.Controllers;

[ApiController]
[Route("api/admin/facilities")]
[Authorize(Roles = StaffRoles.SuperAdmin)]
public class AdminFacilitiesController : ControllerBase
{
    private readonly IFacilityService _facilityService;

    public AdminFacilitiesController(IFacilityService facilityService) => _facilityService = facilityService;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? q, CancellationToken cancellationToken)
    {
        return Ok(await _facilityService.GetAsync(q, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var data = await _facilityService.GetByIdAsync(id, cancellationToken);
        return data == null ? NotFound(new { error = "Facility not found" }) : Ok(data);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] FacilityUpsertDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var data = await _facilityService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = data.Id }, data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] FacilityUpsertDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var data = await _facilityService.UpdateAsync(id, dto, cancellationToken);
            return data == null ? NotFound(new { error = "Facility not found" }) : Ok(data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _facilityService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound(new { error = "Facility not found" });
    }
}

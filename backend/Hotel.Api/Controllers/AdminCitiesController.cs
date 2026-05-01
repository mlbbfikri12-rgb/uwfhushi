using Hotel.Api.DTOs;
using Hotel.Api.Entities.Master;
using Hotel.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hotel.Api.Controllers;

[ApiController]
[Route("api/admin/cities")]
[Authorize(Roles = StaffRoles.SuperAdmin)]
public class AdminCitiesController : ControllerBase
{
    private readonly ICityService _cityService;

    public AdminCitiesController(ICityService cityService) => _cityService = cityService;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? q, CancellationToken cancellationToken)
    {
        return Ok(await _cityService.GetAsync(q, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var data = await _cityService.GetByIdAsync(id, cancellationToken);
        return data == null ? NotFound(new { error = "City not found" }) : Ok(data);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CityUpsertDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var data = await _cityService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = data.Id }, data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CityUpsertDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var data = await _cityService.UpdateAsync(id, dto, cancellationToken);
            return data == null ? NotFound(new { error = "City not found" }) : Ok(data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _cityService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound(new { error = "City not found" });
    }
}

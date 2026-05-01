using Hotel.Api.DTOs;
using Hotel.Api.Entities.Master;
using Hotel.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hotel.Api.Controllers;

[ApiController]
[Route("api/admin/brands")]
[Authorize(Roles = StaffRoles.SuperAdmin)]
public class AdminBrandsController : ControllerBase
{
    private readonly IBrandService _brandService;

    public AdminBrandsController(IBrandService brandService) => _brandService = brandService;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? q, CancellationToken cancellationToken)
    {
        return Ok(await _brandService.GetAsync(q, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var data = await _brandService.GetByIdAsync(id, cancellationToken);
        return data == null ? NotFound(new { error = "Brand not found" }) : Ok(data);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BrandUpsertDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var data = await _brandService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = data.Id }, data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] BrandUpsertDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var data = await _brandService.UpdateAsync(id, dto, cancellationToken);
            return data == null ? NotFound(new { error = "Brand not found" }) : Ok(data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _brandService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound(new { error = "Brand not found" });
    }
}

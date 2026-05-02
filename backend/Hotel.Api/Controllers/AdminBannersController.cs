using Hotel.Api.DTOs;
using Hotel.Api.Entities.Master;
using Hotel.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hotel.Api.Controllers;

[ApiController]
[Route("api/admin/banners")]
[Authorize(Roles = StaffRoles.SuperAdmin)]
public class AdminBannersController : ControllerBase
{
    private readonly IBannerService _bannerService;

    public AdminBannersController(IBannerService bannerService)
    {
        _bannerService = bannerService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        return Ok(await _bannerService.GetAllAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertHeroBannerDto dto, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _bannerService.CreateAsync(dto, cancellationToken));
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertHeroBannerDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _bannerService.UpdateAsync(id, dto, cancellationToken);
            return result == null ? NotFound(new { error = "Banner not found" }) : Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _bannerService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound(new { error = "Banner not found" });
    }
}

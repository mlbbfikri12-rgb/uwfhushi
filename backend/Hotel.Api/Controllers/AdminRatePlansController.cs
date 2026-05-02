using Hotel.Api.DTOs;
using Hotel.Api.Entities.Master;
using Hotel.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hotel.Api.Controllers;

[ApiController]
[Authorize(Roles = $"{StaffRoles.SuperAdmin},{StaffRoles.SPV}")]
public class AdminRatePlansController : ControllerBase
{
    private readonly IRatePlanAdminService _ratePlanAdminService;

    public AdminRatePlansController(IRatePlanAdminService ratePlanAdminService)
    {
        _ratePlanAdminService = ratePlanAdminService;
    }

    [HttpGet("api/admin/room-types/{roomTypeId:guid}/rate-plans")]
    public async Task<IActionResult> GetByRoomType(Guid roomTypeId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _ratePlanAdminService.GetByRoomTypeAsync(roomTypeId, cancellationToken));
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("api/admin/room-types/{roomTypeId:guid}/rate-plans")]
    public async Task<IActionResult> Create(Guid roomTypeId, [FromBody] UpsertRatePlanDto dto, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _ratePlanAdminService.CreateAsync(roomTypeId, dto, cancellationToken));
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("api/admin/rate-plans/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertRatePlanDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _ratePlanAdminService.UpdateAsync(id, dto, cancellationToken);
            return result == null ? NotFound(new { error = "Rate plan not found" }) : Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("api/admin/rate-plans/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _ratePlanAdminService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound(new { error = "Rate plan not found" });
    }
}

using Hotel.Api.DTOs;
using Hotel.Api.Entities.Master;
using Hotel.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hotel.Api.Controllers;

[ApiController]
[Route("api/admin/bookings")]
[Authorize(Roles = $"{StaffRoles.SuperAdmin},{StaffRoles.SPV},{StaffRoles.FO}")]
public class AdminBookingsController : ControllerBase
{
    private readonly IAdminBookingService _adminBookingService;

    public AdminBookingsController(IAdminBookingService adminBookingService)
    {
        _adminBookingService = adminBookingService;
    }

    [HttpGet("groups")]
    public async Task<IActionResult> GetGroups(
        [FromQuery] AdminBookingGroupQueryDto query,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _adminBookingService.GetBookingGroupsAsync(query, cancellationToken));
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("groups/{id:guid}")]
    public async Task<IActionResult> GetGroupById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _adminBookingService.GetBookingGroupByIdAsync(id, cancellationToken);
            return result == null ? NotFound(new { error = "Booking group not found" }) : Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("payment-events")]
    public async Task<IActionResult> GetPaymentEvents(
        [FromQuery] AdminPaymentEventQueryDto query,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _adminBookingService.GetPaymentEventsAsync(query, cancellationToken));
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

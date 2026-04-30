using Hotel.Api.DTOs;
using Hotel.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Hotel.Api.Controllers;

[ApiController]
[Route("api/public/rooms")]
public class PublicRoomsController : ControllerBase
{
    private readonly IRoomManagementService _roomManagementService;

    public PublicRoomsController(IRoomManagementService roomManagementService)
    {
        _roomManagementService = roomManagementService;
    }

    [HttpPost("availability/search")]
    public async Task<IActionResult> SearchAvailableRooms([FromBody] AvailabilitySearchDto dto)
    {
        try
        {
            var rooms = await _roomManagementService.SearchAvailableRoomsAsync(dto);
            return Ok(rooms);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

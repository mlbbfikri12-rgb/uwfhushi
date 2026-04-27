using Hotel.Api.DTOs;
using Hotel.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hotel.Api.Controllers;

[ApiController]
[Route("api/room-types")]
[Authorize]
public class RoomTypesController : ControllerBase
{
    private readonly IRoomManagementService _roomManagementService;

    public RoomTypesController(IRoomManagementService roomManagementService)
    {
        _roomManagementService = roomManagementService;
    }

    [HttpGet]
    public async Task<IActionResult> GetRoomTypes()
    {
        var roomTypes = await _roomManagementService.GetRoomTypesAsync();
        return Ok(roomTypes);
    }

    [HttpPost]
    [Authorize(Roles = "SPV")]
    public async Task<IActionResult> CreateRoomType([FromBody] CreateRoomTypeDto dto)
    {
        try
        {
            var roomType = await _roomManagementService.CreateRoomTypeAsync(dto);
            return CreatedAtAction(nameof(GetRoomTypes), new { id = roomType.Id }, roomType);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SPV")]
    public async Task<IActionResult> UpdateRoomType(Guid id, [FromBody] UpdateRoomTypeDto dto)
    {
        try
        {
            var roomType = await _roomManagementService.UpdateRoomTypeAsync(id, dto);
            return roomType == null ? NotFound(new { error = "Room type not found" }) : Ok(roomType);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

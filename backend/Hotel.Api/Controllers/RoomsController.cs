using Hotel.Api.DTOs;
using Hotel.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hotel.Api.Controllers;

[ApiController]
[Route("api/rooms")]
[Authorize]
public class RoomsController : ControllerBase
{
    private readonly IRoomManagementService _roomManagementService;

    public RoomsController(IRoomManagementService roomManagementService)
    {
        _roomManagementService = roomManagementService;
    }

    [HttpGet]
    public async Task<IActionResult> GetRooms()
    {
        var rooms = await _roomManagementService.GetRoomsAsync();
        return Ok(rooms);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetRoom(Guid id)
    {
        var room = await _roomManagementService.GetRoomByIdAsync(id);
        return room == null ? NotFound(new { error = "Room not found" }) : Ok(room);
    }

    [HttpPost]
    [Authorize(Roles = "SPV,FO")]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomDto dto)
    {
        try
        {
            var room = await _roomManagementService.CreateRoomAsync(dto);
            return CreatedAtAction(nameof(GetRoom), new { id = room.Id }, room);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SPV,FO")]
    public async Task<IActionResult> UpdateRoom(Guid id, [FromBody] UpdateRoomDto dto)
    {
        try
        {
            var room = await _roomManagementService.UpdateRoomAsync(id, dto);
            return room == null ? NotFound(new { error = "Room not found" }) : Ok(room);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "SPV,FO")]
    public async Task<IActionResult> UpdateRoomStatus(Guid id, [FromBody] UpdateRoomStatusDto dto)
    {
        try
        {
            var room = await _roomManagementService.UpdateRoomStatusAsync(id, dto.Status);
            return room == null ? NotFound(new { error = "Room not found" }) : Ok(room);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/operational-status")]
    [Authorize(Roles = "SPV,FO")]
    public async Task<IActionResult> UpdateOperationalStatus(Guid id, [FromBody] UpdateRoomOperationalStatusDto dto)
    {
        try
        {
            var room = await _roomManagementService.UpdateRoomOperationalStatusAsync(id, dto.OperationalStatus);
            return room == null ? NotFound(new { error = "Room not found" }) : Ok(room);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/checkout")]
    [Authorize(Roles = "SPV,FO")]
    public async Task<IActionResult> MarkCheckout(Guid id)
    {
        try
        {
            var room = await _roomManagementService.MarkRoomCheckedOutAsync(id);
            return room == null ? NotFound(new { error = "Room not found" }) : Ok(room);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/cleaning")]
    [Authorize(Roles = "SPV,FO")]
    public async Task<IActionResult> MarkCleaning(Guid id)
    {
        try
        {
            var room = await _roomManagementService.MarkRoomCleaningAsync(id);
            return room == null ? NotFound(new { error = "Room not found" }) : Ok(room);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/clean")]
    [Authorize(Roles = "SPV,FO")]
    public async Task<IActionResult> MarkClean(Guid id)
    {
        try
        {
            var room = await _roomManagementService.MarkRoomCleanAsync(id);
            return room == null ? NotFound(new { error = "Room not found" }) : Ok(room);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{roomId:guid}/images")]
    [Authorize(Roles = "SPV,FO")]
    public async Task<IActionResult> AddRoomImage(Guid roomId, [FromBody] AddRoomImageDto dto)
    {
        try
        {
            var image = await _roomManagementService.AddRoomImageAsync(roomId, dto);
            return image == null ? NotFound(new { error = "Room not found" }) : Ok(image);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{roomId:guid}/images/{imageId:guid}")]
    [Authorize(Roles = "SPV,FO")]
    public async Task<IActionResult> DeleteRoomImage(Guid roomId, Guid imageId)
    {
        var deleted = await _roomManagementService.DeleteRoomImageAsync(roomId, imageId);
        return deleted ? NoContent() : NotFound(new { error = "Room image not found" });
    }

    [HttpPut("{roomId:guid}/availability")]
    [Authorize(Roles = "SPV,FO")]
    public async Task<IActionResult> SetAvailability(Guid roomId, [FromBody] UpdateRoomAvailabilityDto dto)
    {
        try
        {
            var availability = await _roomManagementService.SetAvailabilityAsync(roomId, dto);
            return Ok(availability);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("availability/search")]
    [AllowAnonymous]
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

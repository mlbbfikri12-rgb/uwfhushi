using Hotel.Api.DTOs;
using Hotel.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hotel.Api.Controllers;

[ApiController]
[Route("api/staff")]
[Authorize(Roles = "SUPER_ADMIN")]
public class StaffController : ControllerBase
{
    private readonly IStaffAdminService _staffAdminService;

    public StaffController(IStaffAdminService staffAdminService)
    {
        _staffAdminService = staffAdminService;
    }

    [HttpGet]
    public async Task<IActionResult> GetStaffs()
    {
        var staffs = await _staffAdminService.GetStaffsAsync();
        return Ok(staffs);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetStaff(Guid id)
    {
        var staff = await _staffAdminService.GetStaffByIdAsync(id);
        return staff == null ? NotFound(new { error = "Staff not found" }) : Ok(staff);
    }

    [HttpPost]
    public async Task<IActionResult> CreateStaff([FromBody] CreateStaffDto dto)
    {
        try
        {
            var staff = await _staffAdminService.CreateStaffAsync(dto);
            return CreatedAtAction(nameof(GetStaff), new { id = staff.Id }, staff);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStaffStatus(Guid id, [FromBody] UpdateStaffStatusDto dto)
    {
        var staff = await _staffAdminService.UpdateStaffStatusAsync(id, dto.IsActive);
        return staff == null ? NotFound(new { error = "Staff not found" }) : Ok(staff);
    }

    [HttpPost("{staffId:guid}/branches/{branchId:guid}")]
    public async Task<IActionResult> AssignBranch(Guid staffId, Guid branchId)
    {
        try
        {
            var staff = await _staffAdminService.AssignBranchAsync(staffId, branchId);
            return staff == null ? NotFound(new { error = "Staff not found" }) : Ok(staff);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{staffId:guid}/branches/{branchId:guid}")]
    public async Task<IActionResult> RemoveBranch(Guid staffId, Guid branchId)
    {
        var staff = await _staffAdminService.RemoveBranchAsync(staffId, branchId);
        return staff == null ? NotFound(new { error = "Staff not found" }) : Ok(staff);
    }
}

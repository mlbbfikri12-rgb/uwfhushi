using Hotel.Api.DTOs;
using Hotel.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hotel.Api.Controllers;

[ApiController]
[Route("api/branches")]
[Authorize(Roles = "SUPER_ADMIN")]
public class BranchesController : ControllerBase
{
    private readonly IBranchProvisioningService _branchProvisioningService;

    public BranchesController(IBranchProvisioningService branchProvisioningService)
    {
        _branchProvisioningService = branchProvisioningService;
    }

    [HttpGet]
    public async Task<IActionResult> GetBranches()
    {
        var branches = await _branchProvisioningService.GetBranchesAsync();
        return Ok(branches);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetBranch(Guid id)
    {
        var branch = await _branchProvisioningService.GetBranchByIdAsync(id);
        return branch == null ? NotFound(new { error = "Branch not found" }) : Ok(branch);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBranch([FromBody] CreateBranchDto dto)
    {
        try
        {
            var branch = await _branchProvisioningService.CreateBranchAsync(dto);
            return CreatedAtAction(nameof(GetBranch), new { id = branch.Id }, branch);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateBranchStatus(Guid id, [FromBody] UpdateBranchStatusDto dto)
    {
        var branch = await _branchProvisioningService.UpdateBranchStatusAsync(id, dto.IsActive);
        return branch == null ? NotFound(new { error = "Branch not found" }) : Ok(branch);
    }
}

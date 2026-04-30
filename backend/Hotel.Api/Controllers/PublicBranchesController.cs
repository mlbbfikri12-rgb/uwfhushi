using Hotel.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Hotel.Api.Controllers;

[ApiController]
[Route("api/public/branches")]
public class PublicBranchesController : ControllerBase
{
    private readonly IPublicBranchService _publicBranchService;

    public PublicBranchesController(IPublicBranchService publicBranchService)
    {
        _publicBranchService = publicBranchService;
    }

    [HttpGet]
    public async Task<IActionResult> SearchBranches([FromQuery] string? q, [FromQuery] int limit = 20, CancellationToken cancellationToken = default)
    {
        var branches = await _publicBranchService.SearchBranchesAsync(q, limit, cancellationToken);
        return Ok(branches);
    }
}

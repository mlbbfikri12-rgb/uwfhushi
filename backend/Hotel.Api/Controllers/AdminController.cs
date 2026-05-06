using Hotel.Api.Entities.Master;
using Hotel.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hotel.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = StaffRoles.SuperAdmin)]
public class AdminController : ControllerBase
{
    private readonly ITenantSeedService _tenantSeedService;

    public AdminController(ITenantSeedService tenantSeedService)
    {
        _tenantSeedService = tenantSeedService;
    }

    [HttpPost("seed")]
    public async Task<IActionResult> SeedTenant(CancellationToken cancellationToken)
    {
        var branchCode = HttpContext.Request.Headers["X-Branch-Code"].ToString();

        if (string.IsNullOrWhiteSpace(branchCode))
            return BadRequest(new { error = "X-Branch-Code header is required" });

        await _tenantSeedService.SeedOtaDummyDataAsync(branchCode, cancellationToken);

        return Ok(new { message = "Tenant OTA dummy data seeded successfully" });
    }
}

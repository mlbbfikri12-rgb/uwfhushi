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
        await _tenantSeedService.SeedOtaDummyDataAsync(cancellationToken);
        return Ok(new { message = "Tenant OTA dummy data seeded successfully" });
    }
}

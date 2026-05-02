using Hotel.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Hotel.Api.Controllers;

[ApiController]
[Route("api/public/banners")]
[Route("api/banners")]
public class BannersController : ControllerBase
{
    private readonly IBannerService _bannerService;

    public BannersController(IBannerService bannerService)
    {
        _bannerService = bannerService;
    }

    [HttpGet]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        var banners = await _bannerService.GetActiveBannersAsync(cancellationToken);
        return Ok(banners);
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveLegacy(CancellationToken cancellationToken)
    {
        var banners = await _bannerService.GetActiveBannersAsync(cancellationToken);
        return Ok(banners);
    }
}

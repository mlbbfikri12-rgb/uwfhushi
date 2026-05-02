using Hotel.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Hotel.Api.Controllers;

[ApiController]
[Route("api/public")]
public class PublicHomeController : ControllerBase
{
    private readonly IPublicHomeService _publicHomeService;

    public PublicHomeController(IPublicHomeService publicHomeService)
    {
        _publicHomeService = publicHomeService;
    }

    [HttpGet("home")]
    public async Task<IActionResult> GetHome(CancellationToken cancellationToken)
    {
        return Ok(await _publicHomeService.GetHomeAsync(cancellationToken));
    }

    [HttpGet("blogs")]
    public async Task<IActionResult> GetBlogs(CancellationToken cancellationToken)
    {
        return Ok(await _publicHomeService.GetBlogsAsync(cancellationToken));
    }
}

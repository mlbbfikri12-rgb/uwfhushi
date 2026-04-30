using Hotel.Api.DTOs;
using Hotel.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hotel.Api.Controllers;

[ApiController]
[Route("api/uploads")]
[Authorize(Policy = "StaffOnly")]
public class UploadsController : ControllerBase
{
    private readonly IObjectStorageService _storageService;

    public UploadsController(IObjectStorageService storageService)
    {
        _storageService = storageService;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadImage(
    [FromForm] UploadImageRequestDto request,
    CancellationToken cancellationToken = default)
    {
        if (request.File == null || request.File.Length == 0)
            return BadRequest(new { error = "File is required" });

        try
        {
            var result = await _storageService.UploadImageAsync(
                request.File,
                request.Folder,
                cancellationToken
            );

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

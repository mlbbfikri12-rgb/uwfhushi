using Hotel.Api.DTOs;
using Hotel.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Hotel.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IStaffAuthService _staffAuthService;

    public AuthController(IStaffAuthService staffAuthService)
    {
        _staffAuthService = staffAuthService;
    }

    [HttpPost("staff/login")]
    public async Task<IActionResult> StaffLogin([FromBody] StaffLoginDto dto)
    {
        try
        {
            var response = await _staffAuthService.LoginAsync(dto.Email, dto.Password);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }
}

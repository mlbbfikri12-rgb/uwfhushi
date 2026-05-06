using Hotel.Api.DTOs;
using Hotel.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Hotel.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IStaffAuthService _staffAuthService;
    private readonly IClientAuthService _clientAuthService;

    public AuthController(
        IStaffAuthService staffAuthService,
        IClientAuthService clientAuthService)
    {
        _staffAuthService = staffAuthService;
        _clientAuthService = clientAuthService;
    }

    // =========================
    // REGISTER (NO AUTO LOGIN)
    // =========================
    [HttpPost("register")]
    [EnableRateLimiting("auth-register")]
    public async Task<IActionResult> Register([FromBody] ClientRegisterDto dto)
    {
        try
        {
            await _clientAuthService.RegisterAsync(dto);

            return Ok(new
            {
                message = "Registration successful. Please check your email to verify your account."
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // =========================
    // LOGIN (CHECK VERIFIED)
    // =========================
    [HttpPost("login")]
    [EnableRateLimiting("auth-login")]
    public async Task<IActionResult> Login([FromBody] ClientLoginDto dto)
    {
        try
        {
            var response = await _clientAuthService.LoginAsync(dto);

            SetAuthCookie("customer_token", response.Token);

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    // =========================
    // VERIFY EMAIL
    // =========================
    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromQuery] string token)
    {
        try
        {
            await _clientAuthService.VerifyEmailAsync(token);

            return Ok(new { message = "Email verified successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // =========================
    // ME
    // =========================
    [HttpGet("me")]
    [Authorize(Roles = ClientAuthService.CustomerRole)]
    public async Task<IActionResult> Me()
    {
        var customerIdValue = User.FindFirstValue("customer_id");

        if (!Guid.TryParse(customerIdValue, out var customerId))
            return Unauthorized(new { error = "Invalid customer session" });

        var customer = await _clientAuthService.GetMeAsync(customerId);

        return Ok(customer);
    }

    // =========================
    // STAFF LOGIN
    // =========================
    [HttpPost("staff/login")]
    [EnableRateLimiting("auth-login")]
    public async Task<IActionResult> StaffLogin([FromBody] StaffLoginDto dto)
    {
        try
        {
            var response = await _staffAuthService.LoginAsync(dto.Email, dto.Password);

            SetAuthCookie("staff_token", response.Token);

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    [HttpGet("staff/me")]
    [Authorize(Roles = "SUPER_ADMIN,SPV,FO")]
    public async Task<IActionResult> StaffMe()
    {
        var staffIdValue = User.FindFirstValue("staff_id");

        if (!Guid.TryParse(staffIdValue, out var staffId))
            return Unauthorized(new { error = "Invalid staff session" });

        var staff = await _staffAuthService.GetMeAsync(staffId);

        return Ok(staff);
    }

    // =========================
    // COOKIE
    // =========================
    private void SetAuthCookie(string name, string token)
    {
        Response.Cookies.Append(name, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddHours(8)
        });
    }
}
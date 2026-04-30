using Microsoft.AspNetCore.Mvc;
using Hotel.Api.Services;
using Hotel.Api.DTOs;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Hotel.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpPost]
    [EnableRateLimiting("booking")]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto dto)
    {
        try
        {
            var customerIdValue = User.FindFirstValue("customer_id");
            var isCustomerSession = User.Identity?.IsAuthenticated == true &&
                                    User.HasClaim("auth_type", "customer");
            var customerGlobalId = Guid.Empty;

            if (isCustomerSession)
            {
                if (!Guid.TryParse(customerIdValue, out var parsedCustomerGlobalId))
                    return Unauthorized(new { error = "Invalid customer session" });

                customerGlobalId = parsedCustomerGlobalId;
            }

            var booking = isCustomerSession
                ? await _bookingService.CreateBookingForCustomerAsync(
                    customerGlobalId,
                    dto.RoomId,
                    dto.CheckIn,
                    dto.CheckOut,
                    dto.AdultCount,
                    dto.ChildCount,
                    dto.PaymentMethod,
                    dto.Notes
                )
                : await _bookingService.CreateBookingAsync(
                    dto.RoomId,
                    dto.CustomerName,
                    dto.CustomerEmail,
                    dto.CustomerPhone,
                    dto.CheckIn,
                    dto.CheckOut,
                    dto.AdultCount,
                    dto.ChildCount,
                    dto.PaymentMethod,
                    dto.Notes
                );

            return Ok(new
            {
                message = "Booking created",
                bookingId = booking.Id,
                bookingCode = booking.BookingCode,
                totalPrice = booking.TotalPrice
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                error = ex.Message
            });
        }
    }
}

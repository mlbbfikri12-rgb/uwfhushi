using Microsoft.AspNetCore.Mvc;
using Hotel.Api.Services;
using Hotel.Api.DTOs;

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
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto dto)
    {
        try
        {
            var booking = await _bookingService.CreateBookingAsync(
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

using Hotel.Api.Data;
using Hotel.Api.DTOs;
using Hotel.Api.Entities.Tenant;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Api.Services;

public interface IPaymentService
{
    Task<Payment> HandleMidtransWebhookAsync(MidtransWebhookDto dto);
}

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _db;

    public PaymentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Payment> HandleMidtransWebhookAsync(MidtransWebhookDto dto)
    {
        var booking = await _db.Bookings
            .FirstOrDefaultAsync(b => b.BookingCode == dto.OrderId);

        if (booking == null)
            throw new Exception("Booking not found");

        var mappedStatus = MapMidtransStatus(dto.TransactionStatus);

        var payment = await _db.Payments
            .FirstOrDefaultAsync(p => p.BookingId == booking.Id);

        if (payment == null)
        {
            payment = new Payment
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                CreatedAt = DateTime.UtcNow
            };
            _db.Payments.Add(payment);
        }

        payment.Amount = dto.GrossAmount > 0 ? dto.GrossAmount : booking.TotalPrice;
        payment.Method = dto.PaymentType;
        payment.Status = mappedStatus;
        payment.TransactionId = dto.TransactionId;
        payment.PaidAt = mappedStatus == "paid" ? DateTime.UtcNow : null;

        booking.PaymentMethod = dto.PaymentType;
        booking.PaymentStatus = mappedStatus;

        if (mappedStatus == "paid")
        {
            booking.Status = "paid";
            booking.PaidAt = payment.PaidAt;
        }
        else if (mappedStatus == "failed")
        {
            booking.Status = "cancelled";

            var checkInDate = DateTime.SpecifyKind(booking.CheckIn.Date, DateTimeKind.Utc);
            var checkOutDate = DateTime.SpecifyKind(booking.CheckOut.Date, DateTimeKind.Utc);
            var dates = Enumerable.Range(0, (checkOutDate - checkInDate).Days)
                .Select(offset => checkInDate.AddDays(offset))
                .ToList();

            var availabilities = await _db.RoomAvailabilities
                .Where(a => a.RoomId == booking.RoomId && dates.Contains(a.Date))
                .ToListAsync();

            foreach (var availability in availabilities)
            {
                availability.IsAvailable = true;
            }
        }

        await _db.SaveChangesAsync();

        return payment;
    }

    private static string MapMidtransStatus(string status)
    {
        return status.Trim().ToLowerInvariant() switch
        {
            "capture" or "settlement" => "paid",
            "deny" or "cancel" or "expire" or "failure" => "failed",
            _ => "pending"
        };
    }
}

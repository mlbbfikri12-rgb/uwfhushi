using Hotel.Api.Data;
using Hotel.Api.DTOs;
using Hotel.Api.Entities.Tenant;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Api.Services;

public interface IPaymentService
{
    Task<Payment> HandleMidtransWebhookAsync(
        string branchCode, // 🔥 WAJIB: webhook harus kirim ini
        MidtransWebhookDto dto,
        CancellationToken ct = default);
}

public class PaymentService : IPaymentService
{
    private readonly ITenantDbFactory _tenantDbFactory;

    public PaymentService(ITenantDbFactory tenantDbFactory)
    {
        _tenantDbFactory = tenantDbFactory;
    }

    public async Task<Payment> HandleMidtransWebhookAsync(
    string branchCode,
    MidtransWebhookDto dto,
    CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(branchCode))
            throw new Exception("BranchCode is required");

        branchCode = branchCode.Trim().ToUpperInvariant();

        await using var _db = await _tenantDbFactory.CreateAsync(branchCode, ct);

        var booking = await _db.Bookings
            .FirstOrDefaultAsync(b => b.BookingCode == dto.OrderId, ct);

        if (booking == null)
            throw new Exception("Booking not found");

        var mappedStatus = MapMidtransStatus(dto.TransactionStatus);

        var payment = await _db.Payments
            .FirstOrDefaultAsync(p => p.BookingId == booking.Id, ct);

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

        // =========================
        // 🔥 SUCCESS PAYMENT
        // =========================
        if (mappedStatus == "paid")
        {
            booking.Status = "paid";
            booking.PaidAt = payment.PaidAt;
        }
        // =========================
        // 🔥 FAILED PAYMENT (OPTIMIZED)
        // =========================
        else if (mappedStatus == "failed")
        {
            booking.Status = "cancelled";

            var checkIn = booking.CheckIn.Date;
            var checkOut = booking.CheckOut.Date;

            // 🔥 INDEX-FRIENDLY QUERY (NO Contains)
            var availabilities = await _db.RoomAvailabilities
                .Where(a =>
                    a.RoomId == booking.RoomId &&
                    a.Date >= checkIn &&
                    a.Date < checkOut)
                .ToListAsync(ct);

            foreach (var availability in availabilities)
            {
                availability.IsAvailable = true;
            }
        }

        await _db.SaveChangesAsync(ct);

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
using Hotel.Api.Data;
using Hotel.Api.DTOs;
using Hotel.Api.Entities.Master;
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
    private readonly IRoomAssignmentService _roomAssignmentService;
    private readonly MasterDbContext _masterDb;
    private readonly IBookingEmailService _bookingEmailService;
    private readonly IEmailQueue _emailQueue;
    private readonly ICacheService _cache;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        ITenantDbFactory tenantDbFactory,
        IRoomAssignmentService roomAssignmentService,
        MasterDbContext masterDb,
        IBookingEmailService bookingEmailService,
        IEmailQueue emailQueue,
        ICacheService cache,
        ILogger<PaymentService> logger)
    {
        _tenantDbFactory = tenantDbFactory;
        _roomAssignmentService = roomAssignmentService;
        _masterDb = masterDb;
        _bookingEmailService = bookingEmailService;
        _emailQueue = emailQueue;
        _cache = cache;
        _logger = logger;
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
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var branch = await _masterDb.Branches
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Code == branchCode, ct);
        var branchName = branch?.Name ?? branchCode;

        var bookings = await _db.Bookings
            .Include(b => b.BookingGroup)
            .Include(b => b.Customer)
            .Include(b => b.RoomType)
            .Include(b => b.Room)
            .Where(b =>
                b.BookingCode == dto.OrderId ||
                (b.BookingGroupId.HasValue && b.BookingGroup != null && b.BookingGroup.GroupCode == dto.OrderId))
            .ToListAsync(ct);

        if (bookings.Count == 0)
            throw new Exception("Booking not found");

        var mappedStatus = MapMidtransStatus(dto.TransactionStatus);
        Payment? lastPayment = null;

        foreach (var booking in bookings)
        {
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

            if (mappedStatus == "paid")
            {
                if (booking.Status is not ("confirmed" or "paid"))
                {
                    await _roomAssignmentService.AssignRoomAfterPaymentAsync(_db, booking, ct);
                }

                booking.Status = "confirmed";
                booking.PaidAt = payment.PaidAt;
                booking.ConfirmedAtUtc = DateTime.UtcNow;
                booking.HoldUntilUtc = DateTime.UtcNow;
                if (booking.BookingGroup != null)
                {
                    booking.BookingGroup.Status = "confirmed";
                    booking.BookingGroup.PaidAt = payment.PaidAt;
                }

                if (booking.ConfirmationEmailSentAtUtc == null)
                {
                    try
                    {
                        var snapshot = booking;
                        _emailQueue.Enqueue(token => _bookingEmailService.SendBookingCreatedAsync(snapshot, branchName, token));
                        booking.ConfirmationEmailSentAtUtc = DateTime.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed enqueue booking confirmation email. BookingId={BookingId}", booking.Id);
                    }
                }
            }
            else if (mappedStatus == "failed")
            {
                booking.Status = "cancelled";
                if (booking.BookingGroup != null)
                {
                    booking.BookingGroup.Status = "cancelled";
                }
                var assignedRoomId = booking.RoomId;
                booking.RoomId = null;

                if (assignedRoomId.HasValue)
                {
                    var checkIn = booking.CheckIn.Date;
                    var checkOut = booking.CheckOut.Date;

                    var availabilities = await _db.RoomAvailabilities
                        .Where(a =>
                            a.RoomId == assignedRoomId.Value &&
                            a.Date >= checkIn &&
                            a.Date < checkOut)
                        .ToListAsync(ct);

                    foreach (var availability in availabilities)
                    {
                        availability.IsAvailable = true;
                    }
                }
            }

            lastPayment = payment;
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        await _cache.RemoveByPrefixAsync($"availability:{branchCode}:");
        await _cache.RemoveByPrefixAsync($"hotel:full:{branchCode}:");

        return lastPayment ?? throw new Exception("Payment not processed");
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

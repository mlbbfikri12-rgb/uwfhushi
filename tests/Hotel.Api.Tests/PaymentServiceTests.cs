using Hotel.Api.DTOs;
using Hotel.Api.Data;
using Hotel.Api.Entities.Tenant;
using Hotel.Api.Services;
using Hotel.Api.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Api.Tests;

public class PaymentServiceTests
{
    [Fact]
    public async Task HandleMidtransWebhook_WhenSettlement_MarksBookingPaidAndCreatesPayment()
    {
        await using var db = TestDb.CreateTenantDb();
        var (_, room) = await TestDb.SeedRoomAsync(db);
        var booking = await SeedBookingAsync(db, room.Id, "BK-TEST-PAID");
        var service = new PaymentService(db);

        var payment = await service.HandleMidtransWebhookAsync(new MidtransWebhookDto
        {
            OrderId = booking.BookingCode,
            TransactionId = "trx-1",
            TransactionStatus = "settlement",
            PaymentType = "bank_transfer",
            GrossAmount = 777000m
        });

        var updatedBooking = await db.Bookings.SingleAsync();
        Assert.Equal("paid", payment.Status);
        Assert.Equal("paid", updatedBooking.Status);
        Assert.Equal("paid", updatedBooking.PaymentStatus);
        Assert.NotNull(updatedBooking.PaidAt);
    }

    [Fact]
    public async Task HandleMidtransWebhook_WhenFailed_CancelsBookingAndReopensAvailability()
    {
        await using var db = TestDb.CreateTenantDb();
        var (_, room) = await TestDb.SeedRoomAsync(db);
        var booking = await SeedBookingAsync(db, room.Id, "BK-TEST-FAILED");
        db.RoomAvailabilities.Add(new RoomAvailability
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id,
            Date = DateTime.SpecifyKind(new DateTime(2026, 8, 1), DateTimeKind.Utc),
            IsAvailable = false,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new PaymentService(db);

        var payment = await service.HandleMidtransWebhookAsync(new MidtransWebhookDto
        {
            OrderId = booking.BookingCode,
            TransactionStatus = "expire",
            PaymentType = "bank_transfer",
            GrossAmount = 777000m
        });

        var updatedBooking = await db.Bookings.SingleAsync();
        Assert.Equal("failed", payment.Status);
        Assert.Equal("cancelled", updatedBooking.Status);
        Assert.True(await db.RoomAvailabilities.Select(a => a.IsAvailable).SingleAsync());
    }

    [Fact]
    public async Task HandleMidtransWebhook_WhenBookingMissing_Throws()
    {
        await using var db = TestDb.CreateTenantDb();
        var service = new PaymentService(db);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.HandleMidtransWebhookAsync(new MidtransWebhookDto
        {
            OrderId = "missing"
        }));

        Assert.Equal("Booking not found", ex.Message);
    }

    private static async Task<Booking> SeedBookingAsync(AppDbContext db, Guid roomId, string bookingCode)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            GlobalCustomerId = Guid.NewGuid(),
            Name = "Customer",
            Email = $"{bookingCode.ToLowerInvariant()}@example.com",
            Phone = "08123",
            CreatedAt = DateTime.UtcNow
        };

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Customer = customer,
            RoomId = roomId,
            CheckIn = DateTime.SpecifyKind(new DateTime(2026, 8, 1), DateTimeKind.Utc),
            CheckOut = DateTime.SpecifyKind(new DateTime(2026, 8, 2), DateTimeKind.Utc),
            AdultCount = 2,
            ChildCount = 0,
            BasePrice = 700000m,
            Tax = 77000m,
            TotalPrice = 777000m,
            Status = "pending",
            PaymentStatus = "pending",
            BookingCode = bookingCode,
            CreatedAt = DateTime.UtcNow
        };

        db.Customers.Add(customer);
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        return booking;
    }
}

using Hotel.Api.Data;
using Hotel.Api.DTOs;
using Hotel.Api.Entities.Tenant;
using Hotel.Api.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Api.Tests;

public class PaymentServiceTests
{
    [Fact]
    public async Task HandleMidtransWebhook_WhenSettlement_ConfirmsBookingCreatesPaymentAndEvent()
    {
        var options = TestDb.CreateTenantOptions();
        await using var db = TestDb.CreateTenantDb(options);
        await using var masterDb = TestDb.CreateMasterDb();
        var (_, room, _) = await TestDb.SeedRoomWithRatePlanAsync(db);
        var booking = await SeedBookingAsync(db, room, "BK-TEST-PAID");
        var service = TestServices.CreatePaymentService(options, masterDb);

        var payment = await service.HandleMidtransWebhookAsync("SBY", new MidtransWebhookDto
        {
            OrderId = booking.BookingCode,
            TransactionId = "trx-1",
            TransactionStatus = "settlement",
            PaymentType = "bank_transfer",
            GrossAmount = 777000m
        });

        await using var assertDb = TestDb.CreateTenantDb(options);
        var updatedBooking = await assertDb.Bookings.SingleAsync();
        var paymentEvent = await assertDb.PaymentEvents.SingleAsync();
        Assert.Equal("paid", payment.Status);
        Assert.Equal("confirmed", updatedBooking.Status);
        Assert.Equal("paid", updatedBooking.PaymentStatus);
        Assert.NotNull(updatedBooking.PaidAt);
        Assert.Equal("processed", paymentEvent.ProcessingStatus);
        Assert.Equal("paid", paymentEvent.MappedStatus);
    }

    [Fact]
    public async Task HandleMidtransWebhook_WhenFailed_CancelsBookingAndReopensAvailability()
    {
        var options = TestDb.CreateTenantOptions();
        await using var db = TestDb.CreateTenantDb(options);
        await using var masterDb = TestDb.CreateMasterDb();
        var (_, room, _) = await TestDb.SeedRoomWithRatePlanAsync(db);
        var booking = await SeedBookingAsync(db, room, "BK-TEST-FAILED");
        db.RoomAvailabilities.Add(new RoomAvailability
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id,
            Date = DateTime.SpecifyKind(new DateTime(2026, 8, 1), DateTimeKind.Utc),
            IsAvailable = false,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = TestServices.CreatePaymentService(options, masterDb);

        var payment = await service.HandleMidtransWebhookAsync("SBY", new MidtransWebhookDto
        {
            OrderId = booking.BookingCode,
            TransactionStatus = "expire",
            PaymentType = "bank_transfer",
            GrossAmount = 777000m
        });

        await using var assertDb = TestDb.CreateTenantDb(options);
        var updatedBooking = await assertDb.Bookings.SingleAsync();
        Assert.Equal("failed", payment.Status);
        Assert.Equal("cancelled", updatedBooking.Status);
        Assert.Null(updatedBooking.RoomId);
        Assert.True(await assertDb.RoomAvailabilities.Select(a => a.IsAvailable).SingleAsync());
        Assert.Equal("processed", await assertDb.PaymentEvents.Select(e => e.ProcessingStatus).SingleAsync());
    }

    [Fact]
    public async Task HandleMidtransWebhook_WhenBookingMissing_StoresFailedEventAndThrows()
    {
        var options = TestDb.CreateTenantOptions();
        await using var masterDb = TestDb.CreateMasterDb();
        var service = TestServices.CreatePaymentService(options, masterDb);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.HandleMidtransWebhookAsync("SBY", new MidtransWebhookDto
        {
            OrderId = "missing",
            TransactionId = "trx-missing",
            TransactionStatus = "settlement",
            PaymentType = "bank_transfer"
        }));

        await using var assertDb = TestDb.CreateTenantDb(options);
        var paymentEvent = await assertDb.PaymentEvents.SingleAsync();
        Assert.Equal("Booking not found", ex.Message);
        Assert.Equal("failed", paymentEvent.ProcessingStatus);
        Assert.Equal("Booking not found", paymentEvent.ErrorMessage);
    }

    private static async Task<Booking> SeedBookingAsync(AppDbContext db, Room room, string bookingCode)
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
            RoomTypeId = room.RoomTypeId,
            RoomId = room.Id,
            CheckIn = DateTime.SpecifyKind(new DateTime(2026, 8, 1), DateTimeKind.Utc),
            CheckOut = DateTime.SpecifyKind(new DateTime(2026, 8, 2), DateTimeKind.Utc),
            AdultCount = 2,
            ChildCount = 0,
            BasePrice = 700000m,
            Tax = 77000m,
            TotalPrice = 777000m,
            Status = "pending",
            PaymentStatus = "pending",
            HoldUntilUtc = DateTime.UtcNow.AddMinutes(15),
            BookingCode = bookingCode,
            CreatedAt = DateTime.UtcNow
        };

        db.Customers.Add(customer);
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        return booking;
    }
}

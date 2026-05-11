using Hotel.Api.Entities.Tenant;
using Hotel.Api.Services;
using Hotel.Api.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Api.Tests;

public class BookingServiceTests
{
    [Fact]
    public async Task CreateBooking_CreatesRoomTypeBasedPendingBooking_AndCalculatesTotal()
    {
        var options = TestDb.CreateTenantOptions();
        await using var seedDb = TestDb.CreateTenantDb(options);
        await using var masterDb = TestDb.CreateMasterDb();
        var (roomType, _, ratePlan) = await TestDb.SeedRoomWithRatePlanAsync(seedDb, basePrice: 550000m);
        var service = TestServices.CreateBookingService(options, masterDb);

        var booking = await service.CreateBookingAsync(
            roomType.Id,
            ratePlan.Id,
            "Budi",
            "BUDI@Example.Com",
            "08123",
            new DateTime(2026, 8, 1),
            new DateTime(2026, 8, 3),
            2,
            0,
            "midtrans",
            "late check-in");

        Assert.Null(booking.RoomId);
        Assert.Equal(roomType.Id, booking.RoomTypeId);
        Assert.Equal("pending", booking.Status);
        Assert.StartsWith("BK-", booking.BookingCode);
        Assert.Equal(1100000m, booking.BasePrice);
        Assert.Equal(121000m, booking.Tax);
        Assert.Equal(1221000m, booking.TotalPrice);
        Assert.Equal(DateTimeKind.Utc, booking.CheckIn.Kind);
        Assert.True(booking.HoldUntilUtc > DateTime.UtcNow);

        var globalCustomer = await masterDb.CustomersGlobal.SingleAsync();
        Assert.Equal("budi@example.com", globalCustomer.Email);

        await using var assertDb = TestDb.CreateTenantDb(options);
        var tenantCustomer = await assertDb.Customers.SingleAsync();
        Assert.Equal(globalCustomer.Id, tenantCustomer.GlobalCustomerId);
        Assert.Empty(await assertDb.RoomAvailabilities.ToListAsync());
    }

    [Fact]
    public async Task CreateBooking_WhenDateRangeInvalid_Throws()
    {
        var options = TestDb.CreateTenantOptions();
        await using var masterDb = TestDb.CreateMasterDb();
        var service = TestServices.CreateBookingService(options, masterDb);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.CreateBookingAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Budi",
            "budi@example.com",
            "08123",
            new DateTime(2026, 8, 3),
            new DateTime(2026, 8, 3),
            2,
            0,
            null,
            null));

        Assert.Equal("Invalid date range", ex.Message);
    }

    [Fact]
    public async Task CreateBooking_WhenRoomTypeInventoryBlocked_Throws()
    {
        var options = TestDb.CreateTenantOptions();
        await using var seedDb = TestDb.CreateTenantDb(options);
        await using var masterDb = TestDb.CreateMasterDb();
        var (roomType, room, ratePlan) = await TestDb.SeedRoomWithRatePlanAsync(seedDb);
        seedDb.RoomAvailabilities.Add(new RoomAvailability
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id,
            Date = DateTime.SpecifyKind(new DateTime(2026, 8, 1), DateTimeKind.Utc),
            IsAvailable = false,
            CreatedAt = DateTime.UtcNow
        });
        await seedDb.SaveChangesAsync();

        var service = TestServices.CreateBookingService(options, masterDb);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateBookingAsync(
            roomType.Id,
            ratePlan.Id,
            "Budi",
            "budi@example.com",
            "08123",
            new DateTime(2026, 8, 1),
            new DateTime(2026, 8, 2),
            2,
            0,
            null,
            null));

        Assert.Equal("Room not available, please try another date", ex.Message);
    }

    [Fact]
    public async Task CreateBooking_WhenGuestCountExceedsCapacity_Throws()
    {
        var options = TestDb.CreateTenantOptions();
        await using var seedDb = TestDb.CreateTenantDb(options);
        await using var masterDb = TestDb.CreateMasterDb();
        var (roomType, _, ratePlan) = await TestDb.SeedRoomWithRatePlanAsync(seedDb, maxAdults: 2, maxChildren: 0);
        var service = TestServices.CreateBookingService(options, masterDb);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.CreateBookingAsync(
            roomType.Id,
            ratePlan.Id,
            "Budi",
            "budi@example.com",
            "08123",
            new DateTime(2026, 8, 1),
            new DateTime(2026, 8, 2),
            3,
            0,
            null,
            null));

        Assert.Equal("Guest count exceeds room capacity", ex.Message);
    }

    [Fact]
    public async Task CreateBooking_WhenRoomTypeDoesNotExistInTenant_Throws()
    {
        var options = TestDb.CreateTenantOptions();
        await using var masterDb = TestDb.CreateMasterDb();
        var service = TestServices.CreateBookingService(options, masterDb);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.CreateBookingAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Budi",
            "budi@example.com",
            "08123",
            new DateTime(2026, 8, 1),
            new DateTime(2026, 8, 2),
            1,
            0,
            null,
            null));

        Assert.Equal("Room type not found", ex.Message);
    }
}

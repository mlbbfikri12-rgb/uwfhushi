using Hotel.Api.Data;
using Hotel.Api.Entities.Tenant;
using Hotel.Api.Services;
using Hotel.Api.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Api.Tests;

public class BookingServiceTests
{
    [Fact]
    public async Task CreateBooking_CreatesGlobalAndTenantCustomer_LocksAvailability_AndCalculatesTotal()
    {
        await using var tenantDb = TestDb.CreateTenantDb();
        await using var masterDb = TestDb.CreateMasterDb();
        var (_, room) = await TestDb.SeedRoomAsync(tenantDb, basePrice: 550000m);
        var service = new BookingService(tenantDb, masterDb);

        var booking = await service.CreateBookingAsync(
            room.Id,
            "Budi",
            "BUDI@Example.Com",
            "08123",
            new DateTime(2026, 5, 1),
            new DateTime(2026, 5, 3),
            2,
            0,
            "midtrans",
            "late check-in");

        Assert.Equal(room.Id, booking.RoomId);
        Assert.Equal("pending", booking.Status);
        Assert.StartsWith("BK-", booking.BookingCode);
        Assert.Equal(1100000m, booking.BasePrice);
        Assert.Equal(121000m, booking.Tax);
        Assert.Equal(1221000m, booking.TotalPrice);
        Assert.Equal(DateTimeKind.Utc, booking.CheckIn.Kind);

        var globalCustomer = await masterDb.CustomersGlobal.SingleAsync();
        Assert.Equal("budi@example.com", globalCustomer.Email);

        var tenantCustomer = await tenantDb.Customers.SingleAsync();
        Assert.Equal(globalCustomer.Id, tenantCustomer.GlobalCustomerId);

        var lockedDates = await tenantDb.RoomAvailabilities
            .Where(a => a.RoomId == room.Id)
            .OrderBy(a => a.Date)
            .ToListAsync();

        Assert.Equal(2, lockedDates.Count);
        Assert.All(lockedDates, availability => Assert.False(availability.IsAvailable));
    }

    [Fact]
    public async Task CreateBooking_WhenDateRangeInvalid_Throws()
    {
        await using var tenantDb = TestDb.CreateTenantDb();
        await using var masterDb = TestDb.CreateMasterDb();
        var (_, room) = await TestDb.SeedRoomAsync(tenantDb);
        var service = new BookingService(tenantDb, masterDb);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.CreateBookingAsync(
            room.Id,
            "Budi",
            "budi@example.com",
            "08123",
            new DateTime(2026, 5, 3),
            new DateTime(2026, 5, 3),
            2,
            0,
            null,
            null));

        Assert.Equal("Invalid date range", ex.Message);
    }

    [Fact]
    public async Task CreateBooking_WhenRoomAlreadyUnavailable_Throws()
    {
        await using var tenantDb = TestDb.CreateTenantDb();
        await using var masterDb = TestDb.CreateMasterDb();
        var (_, room) = await TestDb.SeedRoomAsync(tenantDb);
        tenantDb.RoomAvailabilities.Add(new RoomAvailability
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id,
            Date = DateTime.SpecifyKind(new DateTime(2026, 5, 1), DateTimeKind.Utc),
            IsAvailable = false,
            CreatedAt = DateTime.UtcNow
        });
        await tenantDb.SaveChangesAsync();

        var service = new BookingService(tenantDb, masterDb);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.CreateBookingAsync(
            room.Id,
            "Budi",
            "budi@example.com",
            "08123",
            new DateTime(2026, 5, 1),
            new DateTime(2026, 5, 2),
            2,
            0,
            null,
            null));

        Assert.Equal("Room not available", ex.Message);
    }

    [Fact]
    public async Task CreateBooking_WhenGuestCountExceedsCapacity_Throws()
    {
        await using var tenantDb = TestDb.CreateTenantDb();
        await using var masterDb = TestDb.CreateMasterDb();
        var (_, room) = await TestDb.SeedRoomAsync(tenantDb, maxAdults: 2, maxChildren: 0);
        var service = new BookingService(tenantDb, masterDb);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.CreateBookingAsync(
            room.Id,
            "Budi",
            "budi@example.com",
            "08123",
            new DateTime(2026, 5, 1),
            new DateTime(2026, 5, 2),
            3,
            0,
            null,
            null));

        Assert.Equal("Guest count exceeds room capacity", ex.Message);
    }

    [Fact]
    public async Task CreateBooking_WhenRoomDoesNotExistInTenant_Throws()
    {
        await using var tenantDb = TestDb.CreateTenantDb();
        await using var masterDb = TestDb.CreateMasterDb();
        var service = new BookingService(tenantDb, masterDb);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.CreateBookingAsync(
            Guid.NewGuid(),
            "Budi",
            "budi@example.com",
            "08123",
            new DateTime(2026, 5, 1),
            new DateTime(2026, 5, 2),
            1,
            0,
            null,
            null));

        Assert.Equal("Room not found in this branch", ex.Message);
    }
}

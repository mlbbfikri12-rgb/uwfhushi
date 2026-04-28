using Hotel.Api.DTOs;
using Hotel.Api.Entities.Tenant;
using Hotel.Api.Services;
using Hotel.Api.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Api.Tests;

public class RoomManagementServiceTests
{
    [Fact]
    public async Task CreateRoomType_WithValidData_CreatesPricedRoomType()
    {
        await using var db = TestDb.CreateTenantDb();
        var service = new RoomManagementService(db);

        var roomType = await service.CreateRoomTypeAsync(new CreateRoomTypeDto
        {
            Name = "Suite",
            Description = "Suite room",
            BasePrice = 900000m,
            MaxAdults = 4,
            MaxChildren = 2
        });

        Assert.Equal("Suite", roomType.Name);
        Assert.Equal(900000m, roomType.BasePrice);
        Assert.Equal(1, await db.RoomTypes.CountAsync());
    }

    [Fact]
    public async Task CreateRoomType_WhenPriceInvalid_Throws()
    {
        await using var db = TestDb.CreateTenantDb();
        var service = new RoomManagementService(db);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.CreateRoomTypeAsync(new CreateRoomTypeDto
        {
            Name = "Suite",
            BasePrice = 0,
            MaxAdults = 2,
            MaxChildren = 0
        }));

        Assert.Equal("Base price must be greater than zero", ex.Message);
    }

    [Fact]
    public async Task CreateRoom_RejectsDuplicateRoomNumber()
    {
        await using var db = TestDb.CreateTenantDb();
        var (roomType, _) = await TestDb.SeedRoomAsync(db, roomNumber: "104");
        var service = new RoomManagementService(db);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.CreateRoomAsync(new CreateRoomDto
        {
            RoomNumber = "104",
            RoomTypeId = roomType.Id,
            Status = "available"
        }));

        Assert.Equal("Room number already exists", ex.Message);
    }

    [Fact]
    public async Task UpdateRoomStatus_WithInvalidStatus_Throws()
    {
        await using var db = TestDb.CreateTenantDb();
        var (_, room) = await TestDb.SeedRoomAsync(db);
        var service = new RoomManagementService(db);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.UpdateRoomStatusAsync(room.Id, "dirty"));

        Assert.Equal("Room status must be available, maintenance, or unavailable", ex.Message);
    }

    [Fact]
    public async Task SetAvailability_UpsertsUtcDate()
    {
        await using var db = TestDb.CreateTenantDb();
        var (_, room) = await TestDb.SeedRoomAsync(db);
        var service = new RoomManagementService(db);

        var availability = await service.SetAvailabilityAsync(room.Id, new UpdateRoomAvailabilityDto
        {
            Date = new DateTime(2026, 6, 1),
            IsAvailable = false
        });

        Assert.False(availability.IsAvailable);
        Assert.Equal(DateTimeKind.Utc, availability.Date.Kind);

        await service.SetAvailabilityAsync(room.Id, new UpdateRoomAvailabilityDto
        {
            Date = new DateTime(2026, 6, 1),
            IsAvailable = true
        });

        Assert.Single(db.RoomAvailabilities);
        Assert.True(await db.RoomAvailabilities.Select(a => a.IsAvailable).SingleAsync());
    }

    [Fact]
    public async Task SearchAvailableRooms_ExcludesMaintenanceAndUnavailableDates()
    {
        await using var db = TestDb.CreateTenantDb();
        var (_, availableRoom) = await TestDb.SeedRoomAsync(db, roomNumber: "101", basePrice: 300000m);
        var (_, maintenanceRoom) = await TestDb.SeedRoomAsync(db, roomNumber: "102", status: "maintenance");
        var (_, lockedRoom) = await TestDb.SeedRoomAsync(db, roomNumber: "103");

        db.RoomAvailabilities.Add(new RoomAvailability
        {
            Id = Guid.NewGuid(),
            RoomId = lockedRoom.Id,
            Date = DateTime.SpecifyKind(new DateTime(2026, 7, 1), DateTimeKind.Utc),
            IsAvailable = false,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new RoomManagementService(db);

        var rooms = await service.SearchAvailableRoomsAsync(new AvailabilitySearchDto
        {
            CheckIn = new DateTime(2026, 7, 1),
            CheckOut = new DateTime(2026, 7, 3),
            AdultCount = 2,
            ChildCount = 0
        });

        Assert.Single(rooms);
        Assert.Equal(availableRoom.Id, rooms.Single().Id);
        Assert.DoesNotContain(rooms, room => room.Id == maintenanceRoom.Id);
    }

    [Fact]
    public async Task AddAndDeleteRoomImage_ManagesImages()
    {
        await using var db = TestDb.CreateTenantDb();
        var (_, room) = await TestDb.SeedRoomAsync(db);
        var service = new RoomManagementService(db);

        var image = await service.AddRoomImageAsync(room.Id, new AddRoomImageDto
        {
            Url = "https://example.com/room.webp",
            Format = "WEBP"
        });

        Assert.NotNull(image);
        Assert.Equal("webp", image.Format);

        var deleted = await service.DeleteRoomImageAsync(room.Id, image.Id);

        Assert.True(deleted);
        Assert.Empty(db.RoomImages);
    }
}

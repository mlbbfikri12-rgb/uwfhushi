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
        var options = TestDb.CreateTenantOptions();
        var service = TestServices.CreateRoomManagementService(options);

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
        await using var assertDb = TestDb.CreateTenantDb(options);
        Assert.Equal(1, await assertDb.RoomTypes.CountAsync());
    }

    [Fact]
    public async Task CreateRoomType_WhenPriceInvalid_Throws()
    {
        var options = TestDb.CreateTenantOptions();
        var service = TestServices.CreateRoomManagementService(options);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.CreateRoomTypeAsync(new CreateRoomTypeDto
        {
            Name = "Suite",
            BasePrice = 0,
            MaxAdults = 2,
            MaxChildren = 0
        }));

        Assert.Equal("Price must > 0", ex.Message);
    }

    [Fact]
    public async Task CreateRoom_RejectsDuplicateRoomNumber()
    {
        var options = TestDb.CreateTenantOptions();
        await using var db = TestDb.CreateTenantDb(options);
        var (roomType, _) = await TestDb.SeedRoomAsync(db, roomNumber: "104");
        var service = TestServices.CreateRoomManagementService(options);

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
        var options = TestDb.CreateTenantOptions();
        await using var db = TestDb.CreateTenantDb(options);
        var (_, room) = await TestDb.SeedRoomAsync(db);
        var service = TestServices.CreateRoomManagementService(options);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.UpdateRoomStatusAsync(room.Id, "dirty"));

        Assert.Equal("Invalid room status", ex.Message);
    }

    [Fact]
    public async Task SetAvailability_UpsertsUtcDate()
    {
        var options = TestDb.CreateTenantOptions();
        await using var db = TestDb.CreateTenantDb(options);
        var (_, room) = await TestDb.SeedRoomAsync(db);
        var service = TestServices.CreateRoomManagementService(options);

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

        await using var assertDb = TestDb.CreateTenantDb(options);
        Assert.Single(assertDb.RoomAvailabilities);
        Assert.True(await assertDb.RoomAvailabilities.Select(a => a.IsAvailable).SingleAsync());
    }

    [Fact]
    public async Task SearchAvailableRooms_ExcludesMaintenanceAndUnavailableDates()
    {
        var options = TestDb.CreateTenantOptions();
        await using var db = TestDb.CreateTenantDb(options);
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

        var service = TestServices.CreateRoomManagementService(options);

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
        var options = TestDb.CreateTenantOptions();
        await using var db = TestDb.CreateTenantDb(options);
        var (_, room) = await TestDb.SeedRoomAsync(db);
        var service = TestServices.CreateRoomManagementService(options);

        var image = await service.AddRoomImageAsync(room.Id, new AddRoomImageDto
        {
            Url = "https://example.com/room.webp",
            Format = "WEBP"
        });

        Assert.NotNull(image);
        Assert.Equal("webp", image.Format);

        var deleted = await service.DeleteRoomImageAsync(room.Id, image.Id);

        Assert.True(deleted);
        await using var assertDb = TestDb.CreateTenantDb(options);
        Assert.Empty(assertDb.RoomImages);
    }
}

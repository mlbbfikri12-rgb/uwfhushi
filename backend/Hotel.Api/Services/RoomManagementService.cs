using Hotel.Api.Data;
using Hotel.Api.DTOs;
using Hotel.Api.Entities.Tenant;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Api.Services;

public interface IRoomManagementService
{
    Task<IReadOnlyCollection<RoomTypeResponseDto>> GetRoomTypesAsync();
    Task<RoomTypeResponseDto> CreateRoomTypeAsync(CreateRoomTypeDto dto);
    Task<RoomTypeResponseDto?> UpdateRoomTypeAsync(Guid id, UpdateRoomTypeDto dto);
    Task<IReadOnlyCollection<RoomResponseDto>> GetRoomsAsync();
    Task<RoomResponseDto?> GetRoomByIdAsync(Guid id);
    Task<RoomResponseDto> CreateRoomAsync(CreateRoomDto dto);
    Task<RoomResponseDto?> UpdateRoomAsync(Guid id, UpdateRoomDto dto);
    Task<RoomResponseDto?> UpdateRoomStatusAsync(Guid id, string status);
    Task<RoomImageResponseDto?> AddRoomImageAsync(Guid roomId, AddRoomImageDto dto);
    Task<bool> DeleteRoomImageAsync(Guid roomId, Guid imageId);
    Task<RoomAvailabilityResponseDto> SetAvailabilityAsync(Guid roomId, UpdateRoomAvailabilityDto dto);
    Task<IReadOnlyCollection<RoomResponseDto>> SearchAvailableRoomsAsync(AvailabilitySearchDto dto);
}

public class RoomManagementService : IRoomManagementService
{
    private static readonly HashSet<string> AllowedRoomStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "available",
        "maintenance",
        "unavailable"
    };

    private readonly AppDbContext _db;

    public RoomManagementService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyCollection<RoomTypeResponseDto>> GetRoomTypesAsync()
    {
        return await _db.RoomTypes
            .AsNoTracking()
            .OrderBy(rt => rt.Name)
            .Select(rt => ToRoomTypeDto(rt))
            .ToListAsync();
    }

    public async Task<RoomTypeResponseDto> CreateRoomTypeAsync(CreateRoomTypeDto dto)
    {
        ValidateRoomType(dto.Name, dto.BasePrice, dto.MaxAdults, dto.MaxChildren);

        var roomType = new RoomType
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Description = dto.Description.Trim(),
            BasePrice = dto.BasePrice,
            MaxAdults = dto.MaxAdults,
            MaxChildren = dto.MaxChildren,
            CreatedAt = DateTime.UtcNow
        };

        _db.RoomTypes.Add(roomType);
        await _db.SaveChangesAsync();

        return ToRoomTypeDto(roomType);
    }

    public async Task<RoomTypeResponseDto?> UpdateRoomTypeAsync(Guid id, UpdateRoomTypeDto dto)
    {
        ValidateRoomType(dto.Name, dto.BasePrice, dto.MaxAdults, dto.MaxChildren);

        var roomType = await _db.RoomTypes.FirstOrDefaultAsync(rt => rt.Id == id);
        if (roomType == null)
            return null;

        roomType.Name = dto.Name.Trim();
        roomType.Description = dto.Description.Trim();
        roomType.BasePrice = dto.BasePrice;
        roomType.MaxAdults = dto.MaxAdults;
        roomType.MaxChildren = dto.MaxChildren;

        await _db.SaveChangesAsync();

        return ToRoomTypeDto(roomType);
    }

    public async Task<IReadOnlyCollection<RoomResponseDto>> GetRoomsAsync()
    {
        return await BuildRoomQuery()
            .OrderBy(r => r.RoomNumber)
            .ToListAsync();
    }

    public async Task<RoomResponseDto?> GetRoomByIdAsync(Guid id)
    {
        return await BuildRoomQuery()
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<RoomResponseDto> CreateRoomAsync(CreateRoomDto dto)
    {
        var status = NormalizeRoomStatus(dto.Status);
        var roomNumber = dto.RoomNumber.Trim();

        if (string.IsNullOrWhiteSpace(roomNumber))
            throw new Exception("Room number is required");

        var roomTypeExists = await _db.RoomTypes.AnyAsync(rt => rt.Id == dto.RoomTypeId);
        if (!roomTypeExists)
            throw new Exception("Room type not found");

        var roomExists = await _db.Rooms.AnyAsync(r => r.RoomNumber == roomNumber);
        if (roomExists)
            throw new Exception("Room number already exists");

        var room = new Room
        {
            Id = Guid.NewGuid(),
            RoomNumber = roomNumber,
            RoomTypeId = dto.RoomTypeId,
            Status = status,
            CreatedAt = DateTime.UtcNow
        };

        _db.Rooms.Add(room);
        await _db.SaveChangesAsync();

        return await GetRoomByIdAsync(room.Id)
            ?? throw new Exception("Created room could not be loaded");
    }

    public async Task<RoomResponseDto?> UpdateRoomAsync(Guid id, UpdateRoomDto dto)
    {
        var status = NormalizeRoomStatus(dto.Status);
        var roomNumber = dto.RoomNumber.Trim();

        if (string.IsNullOrWhiteSpace(roomNumber))
            throw new Exception("Room number is required");

        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == id);
        if (room == null)
            return null;

        var duplicate = await _db.Rooms.AnyAsync(r => r.Id != id && r.RoomNumber == roomNumber);
        if (duplicate)
            throw new Exception("Room number already exists");

        var roomTypeExists = await _db.RoomTypes.AnyAsync(rt => rt.Id == dto.RoomTypeId);
        if (!roomTypeExists)
            throw new Exception("Room type not found");

        room.RoomNumber = roomNumber;
        room.RoomTypeId = dto.RoomTypeId;
        room.Status = status;

        await _db.SaveChangesAsync();

        return await GetRoomByIdAsync(id);
    }

    public async Task<RoomResponseDto?> UpdateRoomStatusAsync(Guid id, string status)
    {
        var normalizedStatus = NormalizeRoomStatus(status);
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == id);

        if (room == null)
            return null;

        room.Status = normalizedStatus;
        await _db.SaveChangesAsync();

        return await GetRoomByIdAsync(id);
    }

    public async Task<RoomImageResponseDto?> AddRoomImageAsync(Guid roomId, AddRoomImageDto dto)
    {
        var roomExists = await _db.Rooms.AnyAsync(r => r.Id == roomId);
        if (!roomExists)
            return null;

        if (string.IsNullOrWhiteSpace(dto.Url))
            throw new Exception("Image URL is required");

        var image = new RoomImage
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            Url = dto.Url.Trim(),
            Format = string.IsNullOrWhiteSpace(dto.Format) ? "webp" : dto.Format.Trim().ToLowerInvariant(),
            CreatedAt = DateTime.UtcNow
        };

        _db.RoomImages.Add(image);
        await _db.SaveChangesAsync();

        return new RoomImageResponseDto
        {
            Id = image.Id,
            Url = image.Url,
            Format = image.Format
        };
    }

    public async Task<bool> DeleteRoomImageAsync(Guid roomId, Guid imageId)
    {
        var image = await _db.RoomImages.FirstOrDefaultAsync(i => i.RoomId == roomId && i.Id == imageId);
        if (image == null)
            return false;

        _db.RoomImages.Remove(image);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<RoomAvailabilityResponseDto> SetAvailabilityAsync(Guid roomId, UpdateRoomAvailabilityDto dto)
    {
        var roomExists = await _db.Rooms.AnyAsync(r => r.Id == roomId);
        if (!roomExists)
            throw new Exception("Room not found");

        var date = DateTime.SpecifyKind(dto.Date.Date, DateTimeKind.Utc);
        var availability = await _db.RoomAvailabilities
            .FirstOrDefaultAsync(a => a.RoomId == roomId && a.Date == date);

        if (availability == null)
        {
            availability = new RoomAvailability
            {
                Id = Guid.NewGuid(),
                RoomId = roomId,
                Date = date,
                CreatedAt = DateTime.UtcNow
            };
            _db.RoomAvailabilities.Add(availability);
        }

        availability.IsAvailable = dto.IsAvailable;
        await _db.SaveChangesAsync();

        return new RoomAvailabilityResponseDto
        {
            RoomId = roomId,
            Date = date,
            IsAvailable = availability.IsAvailable
        };
    }

    public async Task<IReadOnlyCollection<RoomResponseDto>> SearchAvailableRoomsAsync(AvailabilitySearchDto dto)
    {
        var checkInDate = DateTime.SpecifyKind(dto.CheckIn.Date, DateTimeKind.Utc);
        var checkOutDate = DateTime.SpecifyKind(dto.CheckOut.Date, DateTimeKind.Utc);

        if (checkOutDate <= checkInDate)
            throw new Exception("Invalid date range");

        if (dto.AdultCount < 1)
            throw new Exception("At least one adult guest is required");

        var dates = Enumerable.Range(0, (checkOutDate - checkInDate).Days)
            .Select(offset => checkInDate.AddDays(offset))
            .ToList();

        return await _db.Rooms
            .AsNoTracking()
            .Where(r =>
                r.Status == "available" &&
                r.RoomType.MaxAdults >= dto.AdultCount &&
                r.RoomType.MaxChildren >= dto.ChildCount &&
                !_db.RoomAvailabilities.Any(a =>
                    a.RoomId == r.Id &&
                    dates.Contains(a.Date) &&
                    !a.IsAvailable))
            .OrderBy(r => r.RoomType.BasePrice)
            .ThenBy(r => r.RoomNumber)
            .Select(r => ToRoomDto(r))
            .ToListAsync();
    }

    private IQueryable<RoomResponseDto> BuildRoomQuery()
    {
        return _db.Rooms
            .AsNoTracking()
            .Select(r => ToRoomDto(r));
    }

    private static RoomResponseDto ToRoomDto(Room room)
    {
        return new RoomResponseDto
        {
            Id = room.Id,
            RoomNumber = room.RoomNumber,
            Status = room.Status,
            RoomType = ToRoomTypeDto(room.RoomType),
            Images = room.Images
                .Select(i => new RoomImageResponseDto
                {
                    Id = i.Id,
                    Url = i.Url,
                    Format = i.Format
                })
                .ToList()
        };
    }

    private static RoomTypeResponseDto ToRoomTypeDto(RoomType roomType)
    {
        return new RoomTypeResponseDto
        {
            Id = roomType.Id,
            Name = roomType.Name,
            Description = roomType.Description,
            BasePrice = roomType.BasePrice,
            MaxAdults = roomType.MaxAdults,
            MaxChildren = roomType.MaxChildren
        };
    }

    private static void ValidateRoomType(string name, decimal basePrice, int maxAdults, int maxChildren)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new Exception("Room type name is required");

        if (basePrice <= 0)
            throw new Exception("Base price must be greater than zero");

        if (maxAdults < 1)
            throw new Exception("Max adults must be at least one");

        if (maxChildren < 0)
            throw new Exception("Max children cannot be negative");
    }

    private static string NormalizeRoomStatus(string status)
    {
        var normalizedStatus = status.Trim().ToLowerInvariant();
        if (!AllowedRoomStatuses.Contains(normalizedStatus))
            throw new Exception("Room status must be available, maintenance, or unavailable");

        return normalizedStatus;
    }
}

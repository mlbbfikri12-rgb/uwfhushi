using Hotel.Api.Configurations;
using Hotel.Api.Data;
using Hotel.Api.DTOs;
using Hotel.Api.Entities.Tenant;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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
        "occupied"
    };

    private readonly ITenantDbFactory _tenantDbFactory;
    private readonly ICacheService _cache;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CacheSettings _cacheSettings;
    private readonly BookingValidationSettings _validationSettings;

    public RoomManagementService(
        ITenantDbFactory tenantDbFactory,
        ICacheService cache,
        IHttpContextAccessor httpContextAccessor,
        IOptions<CacheSettings> cacheSettings,
        IOptions<BookingValidationSettings> validationSettings)
    {
        _tenantDbFactory = tenantDbFactory;
        _cache = cache;
        _httpContextAccessor = httpContextAccessor;
        _cacheSettings = cacheSettings.Value;
        _validationSettings = validationSettings.Value;
    }

    // =========================
    // 🔥 CORE HELPER
    // =========================
    private string GetBranchCode()
    {
        var branchCode = _httpContextAccessor.HttpContext?.Request.Headers["X-Branch-Code"].ToString();

        if (string.IsNullOrWhiteSpace(branchCode))
            throw new Exception("X-Branch-Code header is missing");

        return branchCode.Trim().ToUpperInvariant();
    }

    private async Task<AppDbContext> CreateDbAsync(CancellationToken ct = default)
    {
        var branchCode = GetBranchCode();
        return await _tenantDbFactory.CreateAsync(branchCode, ct);
    }

    // =========================
    // ROOM TYPES
    // =========================
    public async Task<IReadOnlyCollection<RoomTypeResponseDto>> GetRoomTypesAsync()
    {
        await using var db = await CreateDbAsync();

        return await db.RoomTypes
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => ToRoomTypeDto(x))
            .ToListAsync();
    }

    public async Task<bool> DeleteRoomImageAsync(Guid roomId, Guid imageId)
    {
        await using var db = await CreateDbAsync();

        var image = await db.RoomImages
            .FirstOrDefaultAsync(x => x.RoomId == roomId && x.Id == imageId);

        if (image == null)
            return false;

        db.RoomImages.Remove(image);
        await db.SaveChangesAsync();

        await InvalidateAvailabilityCacheAsync();

        return true;
    }

    public async Task<RoomImageResponseDto?> AddRoomImageAsync(Guid roomId, AddRoomImageDto dto)
    {
        await using var db = await CreateDbAsync();

        var exists = await db.Rooms.AnyAsync(x => x.Id == roomId);
        if (!exists)
            return null;

        if (string.IsNullOrWhiteSpace(dto.Url))
            throw new Exception("Image URL is required");

        var entity = new RoomImage
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            Url = dto.Url.Trim(),
            Format = string.IsNullOrWhiteSpace(dto.Format)
                ? "webp"
                : dto.Format.Trim().ToLowerInvariant(),
            CreatedAt = DateTime.UtcNow
        };

        db.RoomImages.Add(entity);
        await db.SaveChangesAsync();

        await InvalidateAvailabilityCacheAsync();

        return new RoomImageResponseDto
        {
            Id = entity.Id,
            Url = entity.Url,
            Format = entity.Format
        };
    }

    public async Task<RoomTypeResponseDto> CreateRoomTypeAsync(CreateRoomTypeDto dto)
    {
        await using var db = await CreateDbAsync();

        ValidateRoomType(dto.Name, dto.BasePrice, dto.MaxAdults, dto.MaxChildren);

        var entity = new RoomType
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Description = dto.Description.Trim(),
            BasePrice = dto.BasePrice,
            MaxAdults = dto.MaxAdults,
            MaxChildren = dto.MaxChildren,
            CreatedAt = DateTime.UtcNow
        };

        db.RoomTypes.Add(entity);
        await db.SaveChangesAsync();

        await InvalidateAvailabilityCacheAsync();

        return ToRoomTypeDto(entity);
    }

    public async Task<RoomTypeResponseDto?> UpdateRoomTypeAsync(Guid id, UpdateRoomTypeDto dto)
    {
        await using var db = await CreateDbAsync();

        var entity = await db.RoomTypes.FirstOrDefaultAsync(x => x.Id == id);
        if (entity == null) return null;

        entity.Name = dto.Name.Trim();
        entity.Description = dto.Description.Trim();
        entity.BasePrice = dto.BasePrice;
        entity.MaxAdults = dto.MaxAdults;
        entity.MaxChildren = dto.MaxChildren;

        await db.SaveChangesAsync();
        await InvalidateAvailabilityCacheAsync();

        return ToRoomTypeDto(entity);
    }

    // =========================
    // ROOMS
    // =========================
    public async Task<IReadOnlyCollection<RoomResponseDto>> GetRoomsAsync()
    {
        await using var db = await CreateDbAsync();

        return await db.Rooms
            .AsNoTracking()
            .Include(x => x.RoomType)   // 🔥 WAJIB
            .Include(x => x.Images)     // 🔥 WAJIB kalau dipakai
            .OrderBy(x => x.RoomNumber)
            .Select(x => MapRoom(x))
            .ToListAsync();
    }

    public async Task<RoomResponseDto?> GetRoomByIdAsync(Guid id)
    {
        await using var db = await CreateDbAsync();

        return await db.Rooms
            .AsNoTracking()
            .Include(x => x.RoomType)
            .Include(x => x.Images)
            .Where(x => x.Id == id)
            .Select(x => MapRoom(x))
            .FirstOrDefaultAsync();
    }

    public async Task<RoomResponseDto> CreateRoomAsync(CreateRoomDto dto)
    {
        await using var db = await CreateDbAsync();

        var entity = new Room
        {
            Id = Guid.NewGuid(),
            RoomNumber = dto.RoomNumber.Trim(),
            RoomTypeId = dto.RoomTypeId,
            Status = NormalizeRoomStatus(dto.Status),
            CreatedAt = DateTime.UtcNow
        };

        db.Rooms.Add(entity);
        await db.SaveChangesAsync();

        await InvalidateAvailabilityCacheAsync();

        return await GetRoomByIdAsync(entity.Id)
            ?? throw new Exception("Room not found");
    }

    public async Task<RoomResponseDto?> UpdateRoomAsync(Guid id, UpdateRoomDto dto)
    {
        await using var db = await CreateDbAsync();

        var entity = await db.Rooms.FirstOrDefaultAsync(x => x.Id == id);
        if (entity == null) return null;

        entity.RoomNumber = dto.RoomNumber.Trim();
        entity.RoomTypeId = dto.RoomTypeId;
        entity.Status = NormalizeRoomStatus(dto.Status);

        await db.SaveChangesAsync();
        await InvalidateAvailabilityCacheAsync();

        return await GetRoomByIdAsync(id);
    }

    public async Task<RoomResponseDto?> UpdateRoomStatusAsync(Guid id, string status)
    {
        await using var db = await CreateDbAsync();

        var entity = await db.Rooms.FirstOrDefaultAsync(x => x.Id == id);
        if (entity == null) return null;

        entity.Status = NormalizeRoomStatus(status);

        await db.SaveChangesAsync();
        await InvalidateAvailabilityCacheAsync();

        return await GetRoomByIdAsync(id);
    }

    // =========================
    // AVAILABILITY
    // =========================
    public async Task<RoomAvailabilityResponseDto> SetAvailabilityAsync(Guid roomId, UpdateRoomAvailabilityDto dto)
    {
        await using var db = await CreateDbAsync();

        var date = DateTime.SpecifyKind(dto.Date.Date, DateTimeKind.Utc);

        var entity = await db.RoomAvailabilities
            .FirstOrDefaultAsync(x => x.RoomId == roomId && x.Date == date);

        if (entity == null)
        {
            entity = new RoomAvailability
            {
                Id = Guid.NewGuid(),
                RoomId = roomId,
                Date = date,
                CreatedAt = DateTime.UtcNow
            };
            db.RoomAvailabilities.Add(entity);
        }

        entity.IsAvailable = dto.IsAvailable;

        await db.SaveChangesAsync();
        await InvalidateAvailabilityCacheAsync();

        return new RoomAvailabilityResponseDto
        {
            RoomId = roomId,
            Date = date,
            IsAvailable = entity.IsAvailable
        };
    }

    // =========================
    // SEARCH
    // =========================
    public async Task<IReadOnlyCollection<RoomResponseDto>> SearchAvailableRoomsAsync(AvailabilitySearchDto dto)
    {
        await using var db = await CreateDbAsync();

        var checkIn = dto.CheckIn.Date;
        var checkOut = dto.CheckOut.Date;

        if (checkOut <= checkIn)
            throw new Exception("Invalid date range");

        // =========================
        // 🔥 STEP 1: ambil roomId yang available (INDEX FRIENDLY)
        // =========================
        var availableRoomIds = (await db.Rooms
            .AsNoTracking()
            .Where(r =>
                r.Status == "available" &&
                !db.RoomAvailabilities.Any(a =>
                    a.RoomId == r.Id &&
                    a.Date >= checkIn &&
                    a.Date < checkOut &&
                    !a.IsAvailable))
            .Select(r => r.Id)
            .ToListAsync())
            .ToHashSet(); // 🔥 O(1)

        // =========================
        // 🔥 STEP 2: ambil room detail
        // =========================
        var rooms = await db.Rooms
            .AsNoTracking()
            .Include(x => x.RoomType)
            .Include(x => x.Images)
            .Where(r => availableRoomIds.Contains(r.Id))
            .Select(r => MapRoom(r))
            .ToListAsync();

        return rooms;
    }

    // =========================
    // MAPPERS
    // =========================
    private static RoomResponseDto MapRoom(Room r)
    {
        if (r.RoomType == null)
            throw new Exception("RoomType not loaded");

        return new RoomResponseDto
        {
            Id = r.Id,
            RoomNumber = r.RoomNumber,
            Status = r.Status,

            RoomType = new RoomTypeResponseDto
            {
                Id = r.RoomType.Id,
                Name = r.RoomType.Name,
                Description = r.RoomType.Description,
                BasePrice = r.RoomType.BasePrice,
                MaxAdults = r.RoomType.MaxAdults,
                MaxChildren = r.RoomType.MaxChildren
            },

            Images = r.Images?
                .Select(i => new RoomImageResponseDto
                {
                    Id = i.Id,
                    Url = i.Url,
                    Format = i.Format
                })
                .ToList() ?? new List<RoomImageResponseDto>()
        };
    }
    private static RoomTypeResponseDto ToRoomTypeDto(RoomType rt)
    {
        return new RoomTypeResponseDto
        {
            Id = rt.Id,
            Name = rt.Name,
            Description = rt.Description,
            BasePrice = rt.BasePrice,
            MaxAdults = rt.MaxAdults,
            MaxChildren = rt.MaxChildren
        };
    }

    private static string NormalizeRoomStatus(string status)
    {
        var s = status.Trim().ToLowerInvariant();
        if (!AllowedRoomStatuses.Contains(s))
            throw new Exception("Invalid room status");
        return s;
    }

    private Task InvalidateAvailabilityCacheAsync()
    {
        var branchCode = GetBranchCode();
        return _cache.RemoveByPrefixAsync($"availability:{branchCode}:");
    }

    private static void ValidateRoomType(string name, decimal price, int adult, int child)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new Exception("Name required");

        if (price <= 0)
            throw new Exception("Price must > 0");
    }
}
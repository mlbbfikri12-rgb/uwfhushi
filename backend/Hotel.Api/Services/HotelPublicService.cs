using Hotel.Api.Configurations;
using Hotel.Api.Data;
using Hotel.Api.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Hotel.Api.Services;

public interface IHotelPublicService
{
    Task<HotelFullDto> GetHotelFullAsync(
        string branch,
        DateTime checkIn,
        DateTime checkOut,
        int adultCount,
        int childCount,
        CancellationToken cancellationToken = default);
}

public class HotelPublicService : IHotelPublicService
{
    private readonly MasterDbContext _masterDb;
    private readonly ICacheService _cache;
    private readonly CacheSettings _cacheSettings;
    private readonly BookingValidationSettings _validationSettings;

    public HotelPublicService(
        MasterDbContext masterDb,
        ICacheService cache,
        IOptions<CacheSettings> cacheSettings,
        IOptions<BookingValidationSettings> validationSettings)
    {
        _masterDb = masterDb;
        _cache = cache;
        _cacheSettings = cacheSettings.Value;
        _validationSettings = validationSettings.Value;
    }

    public async Task<HotelFullDto> GetHotelFullAsync(
        string branch,
        DateTime checkIn,
        DateTime checkOut,
        int adultCount,
        int childCount,
        CancellationToken cancellationToken = default)
    {
        var branchCode = branch.Trim().ToUpperInvariant();
        var checkInDate = DateTime.SpecifyKind(checkIn.Date, DateTimeKind.Utc);
        var checkOutDate = DateTime.SpecifyKind(checkOut.Date, DateTimeKind.Utc);

        ValidateRequest(branchCode, checkInDate, checkOutDate, adultCount, childCount);

        var cacheKey = $"hotel:full:{branchCode}:{checkInDate:yyyyMMdd}:{checkOutDate:yyyyMMdd}:adult:{adultCount}:child:{childCount}";
        var cached = await _cache.GetAsync<HotelFullDto>(cacheKey, cancellationToken);
        if (cached != null)
            return cached;

        var branchEntity = await _masterDb.Branches
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Code == branchCode && b.IsActive, cancellationToken);

        if (branchEntity == null)
            throw new Exception("Branch not found");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql($"Host={branchEntity.DbHost};Port={branchEntity.DbPort};Database={branchEntity.DbName};Username={branchEntity.DbUser};Password={branchEntity.DbPassword}")
            .Options;

        await using var tenantDb = new AppDbContext(options);
        var dates = Enumerable.Range(0, (checkOutDate - checkInDate).Days)
            .Select(offset => checkInDate.AddDays(offset))
            .ToList();

        var rooms = await tenantDb.Rooms
            .AsNoTracking()
            .Where(r =>
                r.Status == "available" &&
                r.RoomType.MaxAdults >= adultCount &&
                r.RoomType.MaxChildren >= childCount &&
                !tenantDb.RoomAvailabilities.Any(a =>
                    a.RoomId == r.Id &&
                    dates.Contains(a.Date) &&
                    !a.IsAvailable))
            .OrderBy(r => r.RoomType.BasePrice)
            .ThenBy(r => r.RoomNumber)
            .Select(r => new RoomResponseDto
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
                Images = r.Images
                    .Select(i => new RoomImageResponseDto
                    {
                        Id = i.Id,
                        Url = i.Url,
                        Format = i.Format
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var response = new HotelFullDto
        {
            Branch = new PublicBranchDto
            {
                Id = branchEntity.Id,
                Name = branchEntity.Name,
                Code = branchEntity.Code
            },
            Rooms = rooms,
            CheckIn = checkInDate,
            CheckOut = checkOutDate,
            AdultCount = adultCount,
            ChildCount = childCount
        };

        await _cache.SetAsync(
            cacheKey,
            response,
            TimeSpan.FromMinutes(_cacheSettings.HotelFullTtlMinutes),
            cancellationToken);

        return response;
    }

    private void ValidateRequest(string branchCode, DateTime checkInDate, DateTime checkOutDate, int adultCount, int childCount)
    {
        if (string.IsNullOrWhiteSpace(branchCode))
            throw new Exception("Branch is required");

        var today = DateTime.UtcNow.Date;
        if (checkInDate < today)
            throw new Exception("Check-in cannot be in the past");

        if (checkOutDate <= checkInDate)
            throw new Exception("Invalid date range");

        if ((checkOutDate - checkInDate).Days > _validationSettings.MaxStayNights)
            throw new Exception($"Maximum stay duration is {_validationSettings.MaxStayNights} nights");

        if ((checkInDate - today).Days > _validationSettings.MaxAdvanceBookingDays)
            throw new Exception($"Check-in cannot be more than {_validationSettings.MaxAdvanceBookingDays} days ahead");

        if (adultCount < 1)
            throw new Exception("At least one adult guest is required");

        if (childCount < 0)
            throw new Exception("Child guest count cannot be negative");
    }
}

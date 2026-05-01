using Hotel.Api.Configurations;
using Hotel.Api.Data;
using Hotel.Api.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Hotel.Api.Services;

public interface IHotelPublicService
{
    Task<HotelFullPublicDto> GetHotelFullAsync(
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

    public async Task<HotelFullPublicDto> GetHotelFullAsync(
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
        var cached = await _cache.GetAsync<HotelFullPublicDto>(cacheKey, cancellationToken);
        if (cached != null)
            return cached;

        var hotel = await _masterDb.Hotels
            .AsNoTracking()
            .Include(h => h.City)
            .Include(h => h.Brand)
            .Include(h => h.Images)
            .Include(h => h.HotelFacilities)
            .ThenInclude(hf => hf.Facility)
            .Include(h => h.NearbyPlaces)
            .FirstOrDefaultAsync(h => h.BranchCode == branchCode && h.IsActive, cancellationToken);

        if (hotel == null)
            throw new Exception("Hotel not found");

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

        var roomTypes = await tenantDb.RoomTypes
            .AsNoTracking()
            .Where(rt =>
                rt.MaxAdults >= adultCount &&
                rt.MaxChildren >= childCount &&
                rt.Rooms.Any(r =>
                    r.Status == "available" &&
                    !tenantDb.RoomAvailabilities.Any(a =>
                        a.RoomId == r.Id &&
                        dates.Contains(a.Date) &&
                        !a.IsAvailable)))
            .OrderBy(rt => rt.BasePrice)
            .Select(rt => new HotelRoomTypeDto
            {
                Id = rt.Id,
                Name = rt.Name,
                Image = rt.ImageUrl,
                Size = rt.Size,
                BedType = rt.BedType,
                Capacity = rt.Capacity,
                Description = rt.Description,
                Facilities = rt.Facilities.Select(f => f.Name).ToList(),
                RatePlans = rt.RatePlans
                    .Where(rp => rp.IsActive)
                    .OrderBy(rp => rp.Price)
                    .Select(rp => new RatePlanDto
                    {
                        Id = rp.Id,
                        Name = rp.Name,
                        Price = rp.Price,
                        Benefits = BuildBenefits(rp.IncludesBreakfast, rp.IsRefundable, rp.PaymentType),
                        Terms = rp.TermsConditions
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var response = new HotelFullPublicDto
        {
            Hotel = new PublicHotelMetaDto
            {
                HotelId = hotel.Id,
                BranchCode = hotel.BranchCode,
                Name = hotel.Name,
                Address = hotel.Address,
                City = hotel.City?.Name ?? string.Empty,
                Brand = hotel.Brand?.Name ?? string.Empty,
                Rating = hotel.Rating,
                ReviewCount = hotel.ReviewCount,
                Description = hotel.Description,
                Latitude = hotel.Latitude,
                Longitude = hotel.Longitude
            },
            Images = hotel.Images
                .OrderBy(i => i.SortOrder)
                .Select(i => new HotelImageDto
                {
                    Url = i.Url,
                    Type = i.Type,
                    SortOrder = i.SortOrder
                })
                .ToList(),
            Facilities = hotel.HotelFacilities
                .Select(hf => new FacilityDto
                {
                    Name = hf.Facility!.Name,
                    Icon = hf.Facility.Icon
                })
                .ToList(),
            Nearby = hotel.NearbyPlaces
                .OrderBy(np => np.DistanceKm)
                .Select(np => new NearbyPlaceDto
                {
                    Name = np.Name,
                    DistanceKm = np.DistanceKm
                })
                .ToList(),
            RoomTypes = roomTypes
        };

        await _cache.SetAsync(
            cacheKey,
            response,
            TimeSpan.FromMinutes(_cacheSettings.HotelFullTtlMinutes),
            cancellationToken);

        return response;
    }

    private static string BuildBenefits(bool includesBreakfast, bool isRefundable, string paymentType)
    {
        var benefits = new List<string>();
        if (includesBreakfast) benefits.Add("Breakfast");
        if (isRefundable) benefits.Add("Refundable");
        benefits.Add(paymentType.Equals("pay_at_hotel", StringComparison.OrdinalIgnoreCase)
            ? "Pay at Hotel"
            : "Online Payment");

        return string.Join(", ", benefits);
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

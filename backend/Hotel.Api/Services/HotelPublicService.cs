using Hotel.Api.Data;
using Hotel.Api.Entities.Tenant;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Api.Services;

public interface IHotelPublicService
{
    Task<HotelSummaryDto> GetHotelAsync(string slug, CancellationToken ct);
    Task<List<RoomPricingDto>> GetPricingAsync(string slug, DateTime checkIn, DateTime checkOut, CancellationToken ct);
    Task<RoomDetailDto> GetRoomDetailAsync(string slug, Guid roomTypeId, CancellationToken ct);
}

public class HotelPublicService : IHotelPublicService
{
    private readonly MasterDbContext _masterDb;
    private readonly ICacheService _cache;
    private readonly ITenantDbFactory _tenantDbFactory;
    private readonly IDistributedLockService _lock;

    public HotelPublicService(
        MasterDbContext masterDb,
        ICacheService cache,
        ITenantDbFactory tenantDbFactory,
        IDistributedLockService @lock)
    {
        _masterDb = masterDb;
        _cache = cache;
        _tenantDbFactory = tenantDbFactory;
        _lock = @lock;
    }

    // =========================
    // HELPER: resolve branch
    // =========================
    private async Task<string> GetBranchCodeBySlug(string slug, CancellationToken ct)
    {
        var branchCode = await _masterDb.Hotels
            .AsNoTracking()
            .Where(h => h.Slug == slug && h.IsActive)
            .Select(h => h.BranchCode)
            .FirstOrDefaultAsync(ct);

        if (branchCode == null)
            throw new Exception("Hotel not found");

        return branchCode;
    }

    private IEnumerable<string> mapBenefits(RatePlan ratePlan)
    {
        var benefits = new List<string>();

        if (ratePlan.IncludesBreakfast)
        {
            benefits.Add("BREAKFAST");
        }

        if (ratePlan.PaymentType == "online")
        {
            benefits.Add("ONLINE_PAYMENT");
        }
        else
        {
            benefits.Add("HOTEL_PAYMENT");
        }

        if (ratePlan.IsRefundable)
        {
            benefits.Add("CANCELABLE");
        }
        else
            benefits.Add("NON_REFUNDABLE");
        return benefits;
    }


    // =========================
    // 1. HOTEL SUMMARY
    // =========================
    public async Task<HotelSummaryDto> GetHotelAsync(string slug, CancellationToken ct)
    {
        var cacheKey = $"hotel:summary:{slug}";

        var cached = await _cache.GetAsync<HotelSummaryDto>(cacheKey, ct);
        if (cached != null) return cached;

        await using var handle = await _lock.AcquireAsync(cacheKey);

        cached = await _cache.GetAsync<HotelSummaryDto>(cacheKey, ct);
        if (cached != null) return cached;

        var hotel = await _masterDb.Hotels
    .AsNoTracking()
    .Where(h => h.Slug == slug && h.IsActive)
    .Select(h => new HotelSummaryDto
    {
        Id = h.Id,
        Name = h.Name,
        Description = h.Description,
        City = h.City!.Name,

        Latitude = h.Latitude,
        Longitude = h.Longitude,
        BranchCode = h.BranchCode,

        Images = h.Images
            .OrderBy(i => i.SortOrder)
            .Select(i => i.Url)
            .ToList(),

        Facilities = h.HotelFacilities
            .Select(f => new HotelFacilityDto
            {
                Name = f.Facility!.Name,
                Icon = f.Facility!.Icon
            })
            .ToList(),

        Nearby = h.NearbyPlaces
            .OrderBy(p => p.DistanceKm)
            .Select(p => new NearbyPlaceDto
            {
                Name = p.Name,
                DistanceKm = p.DistanceKm
            })
            .ToList()
    })
    .FirstOrDefaultAsync(ct);

        if (hotel == null)
            throw new Exception("Hotel not found");

        // 🔥 ambil dari precomputed (MASTER)
        hotel.PriceFrom = await _masterDb.HotelPriceSummaries
            .Where(x => x.Slug == slug)
            .Select(x => (decimal?)x.LowestPrice)
            .FirstOrDefaultAsync(ct);

        // ❗ JANGAN fallback ke 0 → biar FE handle
        // hotel.PriceFrom tetap null kalau belum ada

        await _cache.SetAsync(cacheKey, hotel, TimeSpan.FromHours(6), ct);

        return hotel;
    }

    // =========================
    // 2. PRICING
    // =========================
    public async Task<List<RoomPricingDto>> GetPricingAsync(
    string slug,
    DateTime checkIn,
    DateTime checkOut,
    CancellationToken ct)
    {

        checkIn = DateTime.SpecifyKind(checkIn.Date, DateTimeKind.Utc);
        checkOut = DateTime.SpecifyKind(checkOut.Date, DateTimeKind.Utc);
        // ❗ VALIDASI WAJIB
        if (checkOut <= checkIn)
            throw new Exception("Invalid date range");

        var start = checkIn;
        var end = checkOut;

        var cacheKey = $"hotel:pricing:{slug}:{start:yyyyMMdd}:{end:yyyyMMdd}";

        var cached = await _cache.GetAsync<List<RoomPricingDto>>(cacheKey, ct);
        if (cached != null) return cached;

        await using var handle = await _lock.AcquireAsync(cacheKey);

        cached = await _cache.GetAsync<List<RoomPricingDto>>(cacheKey, ct);
        if (cached != null) return cached;

        // 🔥 resolve tenant
        var branchCode = await GetBranchCodeBySlug(slug, ct);
        await using var tenantDb = await _tenantDbFactory.CreateAsync(branchCode, ct);

        // =========================
        // 🔥 STEP 1: ambil available rooms (INDEX FRIENDLY)
        // =========================
        var availableRoomIds = (await tenantDb.Rooms
            .AsNoTracking()
            .Where(r =>
                r.Status == "available" &&
                !tenantDb.RoomAvailabilities.Any(a =>
                    a.RoomId == r.Id &&
                    a.Date >= start &&
                    a.Date < end &&
                    !a.IsAvailable))
            .Select(r => r.Id)
            .ToListAsync(ct))
            .ToHashSet(); // 🔥 O(1) lookup

        // =========================
        // 🔥 STEP 2: ambil roomTypes + ratePlans
        // =========================
        var roomTypes = await tenantDb.RoomTypes
    .AsNoTracking()
    .Where(rt => rt.RatePlans.Any(rp => rp.IsActive)) // 🔥 FILTER PENTING
    .Select(rt => new
    {
        rt.Id,
        rt.Name,
        rt.Description,
        rt.Size,
        rt.BedType,
        rt.Capacity,
        rt.MaxAdults,
        rt.MaxChildren,
        rt.BasePrice,
        rt.ImageUrl,
        facilities = rt.Facilities.Select(f => f.Name),
        Rooms = rt.Rooms
            .Where(r => r.Status == "available")
            .Select(r => r.Id),
        RatePlans = rt.RatePlans
            .Where(rp => rp.IsActive)
    })
    .ToListAsync(ct);

        // =========================
        // 🔥 STEP 3: mapping + compute
        // =========================
        var result = roomTypes
            .Select(rt =>
            {
                var availableRooms = rt.Rooms
                    .Where(r => availableRoomIds.Contains(r))
                    .ToList();

                var isAvailable = availableRooms.Count > 0;

                var ratePlans = rt.RatePlans
                    .OrderBy(rp => rp.Price)
                    .ThenByDescending(rp => rp.IsRefundable)
                    .Take(5) // 🔥 limit payload
                    .Select(rp => new RatePlanSummaryDto
                    {
                        Id = rp.Id,
                        Name = rp.Name,
                        Price = rp.Price,
                        benefits = mapBenefits(rp),
                        TermsPreview = rp.IsRefundable
                            ? "Free cancellation"
                            : "Non-refundable"
                    })
                    .ToList();

                var lowestPrice = isAvailable && ratePlans.Any()
                    ? ratePlans.Min(rp => rp.Price)
                    : 0;

                return new RoomPricingDto
                {
                    RoomTypeId = rt.Id,
                    Name = rt.Name,
                    Description = rt.Description,
                    Size = rt.Size,
                    BedType = rt.BedType,
                    Capacity = rt.Capacity,
                    MaxAdults = rt.MaxAdults,
                    MaxChildren = rt.MaxChildren,
                    BasePrice = rt.BasePrice,
                    IsAvailable = isAvailable,
                    LowestPrice = lowestPrice,
                    RatePlans = isAvailable ? ratePlans : new List<RatePlanSummaryDto>(),
                    Image = rt.ImageUrl,
                    Facilities = rt.facilities
                };
            })
            .OrderBy(r => r.LowestPrice)
            .ToList();

        //cache sebentar aja karena ini cukup berat, dan data relatif tidak sering berubah
        await _cache.SetAsync(cacheKey, result, TimeSpan.FromSeconds(20), ct);

        return result;
    }
    // =========================
    // 3. ROOM DETAIL
    // =========================
    public async Task<RoomDetailDto> GetRoomDetailAsync(string slug, Guid roomTypeId, CancellationToken ct)
    {
        var branchCode = await GetBranchCodeBySlug(slug, ct);

        await using var tenantDb = await _tenantDbFactory.CreateAsync(branchCode, ct);

        // ❗ VALIDASI biar tidak cross-hotel
        var exists = await tenantDb.RoomTypes
            .AnyAsync(rt => rt.Id == roomTypeId, ct);

        if (!exists)
            throw new Exception("Room not found for this hotel");

        var room = await tenantDb.RoomTypes
            .AsNoTracking()
            .Where(rt => rt.Id == roomTypeId)
            .Select(rt => new RoomDetailDto
            {
                Id = rt.Id,
                Name = rt.Name,
                Description = rt.Description,
                Image = rt.ImageUrl,
                BedType = rt.BedType,
                MaxAdults = rt.MaxAdults,
                MaxChildren = rt.MaxChildren,
                Size = rt.Size,
                Capacity = rt.Capacity,
                RatePlans = rt.RatePlans
                    .Where(rp => rp.IsActive)
                    .Select(rp => new RatePlanDetailDto
                    {
                        Id = rp.Id,
                        Name = rp.Name,
                        Price = rp.Price,
                        isbreakFast = rp.IncludesBreakfast,
                        isRefundable = rp.IsRefundable,
                        Terms = rp.TermsConditions
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (room == null)
            throw new Exception("Room not found");

        return room;
    }
}

// =========================
// DTOs
// =========================

public class HotelSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string City { get; set; } = default!;

    // 🔥 NEW
    public string Description { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string BranchCode { get; set; } = default!;

    public List<string> Images { get; set; } = new();
    public List<HotelFacilityDto> Facilities { get; set; } = new();

    // 🔥 NEW
    public List<NearbyPlaceDto> Nearby { get; set; } = new();

    public decimal? PriceFrom { get; set; }
    public string PriceType { get; set; } = "estimate";
}

public class NearbyPlaceDto
{
    public string Name { get; set; } = default!;
    public decimal DistanceKm { get; set; }
}

public class HotelFacilityDto
{
    public string Name { get; set; } = default!;
    public string Icon { get; set; } = default!;
}

public class RoomPricingDto
{
    public Guid RoomTypeId { get; set; }
    public string Name { get; set; } = default!;
    public decimal BasePrice { get; set; }
    public string Image { get; set; } = default!;
    public bool IsAvailable { get; set; }
    public decimal LowestPrice { get; set; }
    public string Description { get; set; } = default!;
    public decimal Size { get; set; }
    public string BedType { get; set; } = default!;
    public int Capacity { get; set; }
    public int MaxAdults { get; set; }
    public int MaxChildren { get; set; }
    public IEnumerable<string> Facilities { get; set; } = new string[0];

    public List<RatePlanSummaryDto> RatePlans { get; set; } = new();
}

public class RatePlanSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public decimal Price { get; set; }
    public IEnumerable<string> benefits { get; set; } = new string[0];
    public string TermsPreview { get; set; } = default!;
}

public class RoomDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Image { get; set; } = default!;
    public decimal Size { get; set; }
    public string BedType { get; set; } = default!;
    public int Capacity { get; set; }
    public int MaxAdults { get; set; }
    public int MaxChildren { get; set; }
    public List<RatePlanDetailDto> RatePlans { get; set; } = new();
}

public class RatePlanDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public decimal Price { get; set; }
    public bool isbreakFast { get; set; }
    public bool isRefundable { get; set; }
    public string Terms { get; set; } = default!;
}
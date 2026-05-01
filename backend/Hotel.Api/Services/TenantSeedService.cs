using Hotel.Api.Data;
using Hotel.Api.Entities.Master;
using Hotel.Api.Entities.Tenant;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Api.Services;

public interface ITenantSeedService
{
    Task SeedOtaDummyDataAsync(CancellationToken cancellationToken = default);
}

public class TenantSeedService : ITenantSeedService
{
    private readonly AppDbContext _tenantDb;
    private readonly MasterDbContext _masterDb;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantSeedService(AppDbContext tenantDb, MasterDbContext masterDb, IHttpContextAccessor httpContextAccessor)
    {
        _tenantDb = tenantDb;
        _masterDb = masterDb;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task SeedOtaDummyDataAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var roomTypes = await EnsureRoomTypesAsync(now, cancellationToken);
        await EnsureRatePlansAsync(roomTypes, cancellationToken);
        await EnsureRoomTypeFacilitiesAsync(roomTypes, cancellationToken);
        await EnsureRoomsAsync(roomTypes, now, cancellationToken);
        await EnsureRoomAvailabilitiesAsync(now, cancellationToken);
        await EnsureMasterNearbyPlacesAsync(cancellationToken);

        await _tenantDb.SaveChangesAsync(cancellationToken);
        await _masterDb.SaveChangesAsync(cancellationToken);
    }

    private async Task<Dictionary<string, RoomType>> EnsureRoomTypesAsync(DateTime now, CancellationToken cancellationToken)
    {
        var seeds = new[]
        {
            new RoomType
            {
                Id = Guid.NewGuid(),
                Name = "Deluxe Room",
                Description = "Comfort room for urban traveler.",
                ImageUrl = "https://images.unsplash.com/photo-1566665797739-1674de7a421a?w=1000&q=80",
                Size = 28,
                BedType = "Queen Bed",
                Capacity = 2,
                BasePrice = 650000,
                MaxAdults = 2,
                MaxChildren = 1,
                CreatedAt = now
            },
            new RoomType
            {
                Id = Guid.NewGuid(),
                Name = "Executive Room",
                Description = "Larger room with executive amenities.",
                ImageUrl = "https://images.unsplash.com/photo-1590490360182-c33d57733427?w=1000&q=80",
                Size = 34,
                BedType = "King Bed",
                Capacity = 3,
                BasePrice = 950000,
                MaxAdults = 3,
                MaxChildren = 1,
                CreatedAt = now
            },
            new RoomType
            {
                Id = Guid.NewGuid(),
                Name = "Family Suite",
                Description = "Spacious suite for family stay.",
                ImageUrl = "https://images.unsplash.com/photo-1584132967334-10e028bd69f7?w=1000&q=80",
                Size = 48,
                BedType = "King + Twin",
                Capacity = 4,
                BasePrice = 1450000,
                MaxAdults = 4,
                MaxChildren = 2,
                CreatedAt = now
            }
        };

        foreach (var seed in seeds)
        {
            var existing = await _tenantDb.RoomTypes.FirstOrDefaultAsync(rt => rt.Name == seed.Name, cancellationToken);
            if (existing == null)
            {
                _tenantDb.RoomTypes.Add(seed);
            }
            else
            {
                existing.Description = seed.Description;
                existing.ImageUrl = seed.ImageUrl;
                existing.Size = seed.Size;
                existing.BedType = seed.BedType;
                existing.Capacity = seed.Capacity;
                existing.BasePrice = seed.BasePrice;
                existing.MaxAdults = seed.MaxAdults;
                existing.MaxChildren = seed.MaxChildren;
            }
        }

        await _tenantDb.SaveChangesAsync(cancellationToken);
        return await _tenantDb.RoomTypes.ToDictionaryAsync(rt => rt.Name, StringComparer.OrdinalIgnoreCase, cancellationToken);
    }

    private async Task EnsureRatePlansAsync(Dictionary<string, RoomType> roomTypes, CancellationToken cancellationToken)
    {
        foreach (var roomType in roomTypes.Values)
        {
            await EnsureRatePlanAsync(roomType, $"{roomType.Name} - Flexible Breakfast", roomType.BasePrice, true, true, "online", "Free cancellation H-2.", cancellationToken);
            await EnsureRatePlanAsync(roomType, $"{roomType.Name} - Saver Non Refundable", Math.Round(roomType.BasePrice * 0.9m, 0), false, false, "online", "Non refundable booking.", cancellationToken);
            await EnsureRatePlanAsync(roomType, $"{roomType.Name} - Pay at Hotel", Math.Round(roomType.BasePrice * 1.05m, 0), true, true, "pay_at_hotel", "Payment at hotel check-in.", cancellationToken);
        }
    }

    private async Task EnsureRatePlanAsync(
        RoomType roomType,
        string name,
        decimal price,
        bool includesBreakfast,
        bool isRefundable,
        string paymentType,
        string terms,
        CancellationToken cancellationToken)
    {
        var existing = await _tenantDb.RatePlans.FirstOrDefaultAsync(rp => rp.RoomTypeId == roomType.Id && rp.Name == name, cancellationToken);
        if (existing == null)
        {
            _tenantDb.RatePlans.Add(new RatePlan
            {
                Id = Guid.NewGuid(),
                RoomTypeId = roomType.Id,
                Name = name,
                Price = price,
                IncludesBreakfast = includesBreakfast,
                IsRefundable = isRefundable,
                PaymentType = paymentType,
                TermsConditions = terms,
                IsActive = true
            });
            return;
        }

        existing.Price = price;
        existing.IncludesBreakfast = includesBreakfast;
        existing.IsRefundable = isRefundable;
        existing.PaymentType = paymentType;
        existing.TermsConditions = terms;
        existing.IsActive = true;
    }

    private async Task EnsureRoomTypeFacilitiesAsync(Dictionary<string, RoomType> roomTypes, CancellationToken cancellationToken)
    {
        var facilitySeeds = new Dictionary<string, string[]>
        {
            ["Deluxe Room"] = new[] { "WiFi", "AC", "TV", "Shower" },
            ["Executive Room"] = new[] { "WiFi", "AC", "TV", "Desk", "Mini Bar" },
            ["Family Suite"] = new[] { "WiFi", "AC", "TV", "Living Area", "Bathtub" }
        };

        foreach (var pair in facilitySeeds)
        {
            if (!roomTypes.TryGetValue(pair.Key, out var roomType))
                continue;

            foreach (var facilityName in pair.Value)
            {
                var exists = await _tenantDb.RoomTypeFacilities
                    .AnyAsync(f => f.RoomTypeId == roomType.Id && f.Name == facilityName, cancellationToken);
                if (!exists)
                {
                    _tenantDb.RoomTypeFacilities.Add(new RoomTypeFacility
                    {
                        Id = Guid.NewGuid(),
                        RoomTypeId = roomType.Id,
                        Name = facilityName
                    });
                }
            }
        }
    }

    private async Task EnsureRoomsAsync(Dictionary<string, RoomType> roomTypes, DateTime now, CancellationToken cancellationToken)
    {
        var roomSeeds = new[]
        {
            ("301", "Deluxe Room"),
            ("302", "Deluxe Room"),
            ("401", "Executive Room"),
            ("402", "Executive Room"),
            ("501", "Family Suite")
        };

        foreach (var (roomNumber, roomTypeName) in roomSeeds)
        {
            if (!roomTypes.TryGetValue(roomTypeName, out var roomType))
                continue;

            var exists = await _tenantDb.Rooms.AnyAsync(r => r.RoomNumber == roomNumber, cancellationToken);
            if (!exists)
            {
                _tenantDb.Rooms.Add(new Room
                {
                    Id = Guid.NewGuid(),
                    RoomNumber = roomNumber,
                    RoomTypeId = roomType.Id,
                    Status = "available",
                    CreatedAt = now
                });
            }
        }
    }

    private async Task EnsureRoomAvailabilitiesAsync(DateTime now, CancellationToken cancellationToken)
    {
        var roomIds = await _tenantDb.Rooms.Select(r => r.Id).ToListAsync(cancellationToken);
        foreach (var roomId in roomIds)
        {
            for (var day = 0; day < 90; day++)
            {
                var date = now.Date.AddDays(day);
                var exists = await _tenantDb.RoomAvailabilities.AnyAsync(a => a.RoomId == roomId && a.Date == date, cancellationToken);
                if (!exists)
                {
                    _tenantDb.RoomAvailabilities.Add(new RoomAvailability
                    {
                        Id = Guid.NewGuid(),
                        RoomId = roomId,
                        Date = date,
                        IsAvailable = true,
                        CreatedAt = now
                    });
                }
            }
        }
    }

    private async Task EnsureMasterNearbyPlacesAsync(CancellationToken cancellationToken)
    {
        var branchCode = _httpContextAccessor.HttpContext?.Request.Headers["X-Branch-Code"].ToString().Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(branchCode))
            return;

        var hotel = await _masterDb.Hotels.FirstOrDefaultAsync(h => h.BranchCode == branchCode, cancellationToken);
        if (hotel == null)
            return;

        var nearbySeeds = new (string Name, decimal Distance)[]
        {
            ("Main City Mall", 0.6m),
            ("Central Train Station", 1.8m),
            ("International Airport", 24.0m)
        };

        foreach (var (name, distance) in nearbySeeds)
        {
            var exists = await _masterDb.NearbyPlaces.AnyAsync(p => p.HotelId == hotel.Id && p.Name == name, cancellationToken);
            if (!exists)
            {
                _masterDb.NearbyPlaces.Add(new NearbyPlace
                {
                    Id = Guid.NewGuid(),
                    HotelId = hotel.Id,
                    Name = name,
                    DistanceKm = distance
                });
            }
        }
    }
}

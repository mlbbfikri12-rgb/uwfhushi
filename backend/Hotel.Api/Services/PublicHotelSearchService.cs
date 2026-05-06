using Hotel.Api.Configurations;
using Hotel.Api.Data;
using Hotel.Api.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Hotel.Api.Services;

public interface IPublicHotelSearchService
{
    Task<PublicHotelSearchResponseDto> SearchAsync(PublicHotelSearchQueryDto query, CancellationToken cancellationToken = default);
}

public class PublicHotelSearchService : IPublicHotelSearchService
{
    private readonly MasterDbContext _masterDb;
    private readonly ICacheService _cache;
    private readonly CacheSettings _cacheSettings;

    public PublicHotelSearchService(
        MasterDbContext masterDb,
        ICacheService cache,
        IOptions<CacheSettings> cacheSettings)
    {
        _masterDb = masterDb;
        _cache = cache;
        _cacheSettings = cacheSettings.Value;
    }

    public async Task<PublicHotelSearchResponseDto> SearchAsync(
        PublicHotelSearchQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var q = query.Q?.Trim() ?? string.Empty;

        var starsKey = query.Stars is { Length: > 0 }
            ? string.Join(",", query.Stars.OrderBy(x => x))
            : "-";

        var brandsKey = query.Brands is { Length: > 0 }
            ? string.Join(",", query.Brands.OrderBy(x => x))
            : "-";

        var brandNamesKey = query.BrandNames is { Length: > 0 }
            ? string.Join(",", query.BrandNames.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            : "-";

        var cacheKey =
            $"public:hotels:search:{q}:{query.CheckIn:yyyyMMdd}:{query.CheckOut:yyyyMMdd}:{query.TotalRooms}:{query.CityId}:{query.MinPrice}:{query.MaxPrice}:{starsKey}:{brandsKey}:{brandNamesKey}";

        var cached = await _cache.GetAsync<PublicHotelSearchResponseDto>(cacheKey, cancellationToken);
        if (cached != null)
            return cached;

        var normalized = q.ToUpperInvariant();

        var isCityMatch = await _masterDb.Cities
            .AsNoTracking()
            .AnyAsync(c => EF.Functions.ILike(c.Name, $"%{normalized}%"), cancellationToken);

        var isHotelMatch = await _masterDb.Hotels
            .AsNoTracking()
            .AnyAsync(h => h.IsActive &&
                (EF.Functions.ILike(h.Name, $"%{normalized}%") ||
                 EF.Functions.ILike(h.BranchCode, $"%{normalized}%")),
                cancellationToken);

        var responseType = isHotelMatch && !isCityMatch ? "hotel" : "city";

        // =========================
        // QUERY HOTEL MASTER
        // =========================
        var hotelsQuery = _masterDb.Hotels
            .AsNoTracking()
            .Include(h => h.City)
            .Include(h => h.Brand)
            .Include(h => h.Images)
            .Where(h => h.IsActive);

        if (!string.IsNullOrWhiteSpace(normalized))
        {
            hotelsQuery = hotelsQuery.Where(h =>
                EF.Functions.ILike(h.Name, $"%{normalized}%") ||
                EF.Functions.ILike(h.BranchCode, $"%{normalized}%") ||
                EF.Functions.ILike(h.City!.Name, $"%{normalized}%"));
        }

        if (query.CityId.HasValue)
            hotelsQuery = hotelsQuery.Where(h => h.CityId == query.CityId.Value);

        if (query.Brands is { Length: > 0 })
            hotelsQuery = hotelsQuery.Where(h =>
                h.BrandId.HasValue && query.Brands.Contains(h.BrandId.Value));

        else if (query.BrandNames is { Length: > 0 })
            hotelsQuery = hotelsQuery.Where(h =>
                h.Brand != null &&
                query.BrandNames.Any(b => EF.Functions.ILike(h.Brand.Name, b)));

        if (query.Stars is { Length: > 0 })
            hotelsQuery = hotelsQuery.Where(h =>
                query.Stars.Contains((int)Math.Round(h.Rating)));

        var hotels = await hotelsQuery
            .OrderByDescending(h => h.Rating)
            .ThenBy(h => h.Name)
            .ToListAsync(cancellationToken);

        // =========================
        // 🔥 PRICE FROM MASTER (NO TENANT)
        // =========================
        var slugs = hotels.Select(h => h.Slug).ToList();

        var priceMap = await _masterDb.HotelPriceSummaries
            .Where(p => slugs.Contains(p.Slug))
            .ToDictionaryAsync(p => p.Slug, p => p.LowestPrice, cancellationToken);

        // =========================
        // BUILD RESULT
        // =========================
        var list = new List<PublicHotelListItemDto>();

        foreach (var hotel in hotels)
        {
            var priceFrom = priceMap.TryGetValue(hotel.Slug, out var p) ? p : 0m;

            if (query.MinPrice.HasValue && priceFrom < query.MinPrice.Value)
                continue;

            if (query.MaxPrice.HasValue && priceFrom > query.MaxPrice.Value)
                continue;

            list.Add(new PublicHotelListItemDto
            {
                HotelId = hotel.Id,
                Slug = hotel.Slug,
                BranchCode = hotel.BranchCode,
                Name = hotel.Name,
                City = hotel.City?.Name ?? string.Empty,
                Rating = hotel.Rating,
                PriceFrom = priceFrom,
                Image = hotel.Images
                    .OrderBy(i => i.SortOrder)
                    .Select(i => i.Url)
                    .FirstOrDefault() ?? string.Empty,
                Brand = hotel.Brand?.Name ?? string.Empty,
                IsCityMatch = !string.IsNullOrWhiteSpace(normalized) &&
                              (hotel.City?.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
            });
        }

        var response = new PublicHotelSearchResponseDto
        {
            Type = responseType,
            Hotels = list
        };

        await _cache.SetAsync(
            cacheKey,
            response,
            TimeSpan.FromMinutes(_cacheSettings.BranchSearchTtlMinutes),
            cancellationToken);

        return response;
    }
}
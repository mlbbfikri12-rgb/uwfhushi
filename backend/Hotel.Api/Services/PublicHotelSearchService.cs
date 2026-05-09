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

        var normalized = q;

        // =========================
        // BASE QUERY
        // =========================
        var hotelsQuery = _masterDb.Hotels
            .AsNoTracking()
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

        // =========================
        // JOIN PRICE (🔥 penting)
        // =========================
        var queryWithPrice =
            from h in hotelsQuery
            join p in _masterDb.HotelPriceSummaries
                on h.Slug equals p.Slug into priceJoin
            from p in priceJoin.DefaultIfEmpty()
            select new
            {
                h.Id,
                h.Slug,
                h.Name,
                h.BranchCode,
                h.Rating,
                CityName = h.City!.Name,
                BrandName = h.Brand != null ? h.Brand.Name : "",
                Image = h.Images
                    .OrderBy(i => i.SortOrder)
                    .Select(i => i.Url)
                    .FirstOrDefault(),
                PriceFrom = p != null ? p.LowestPrice : 0m
            };

        // =========================
        // FILTER PRICE DI DB
        // =========================
        if (query.MinPrice.HasValue)
            queryWithPrice = queryWithPrice.Where(x => x.PriceFrom >= query.MinPrice.Value);

        if (query.MaxPrice.HasValue)
            queryWithPrice = queryWithPrice.Where(x => x.PriceFrom <= query.MaxPrice.Value);

        // =========================
        // EXECUTE QUERY
        // =========================
        var hotels = await queryWithPrice
            .OrderByDescending(x => x.Rating)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        // =========================
        // BUILD RESULT
        // =========================
        var list = hotels.Select(h => new PublicHotelListItemDto
        {
            HotelId = h.Id,
            Slug = h.Slug,
            BranchCode = h.BranchCode,
            Name = h.Name,
            City = h.CityName,
            Rating = h.Rating,
            PriceFrom = h.PriceFrom,
            Image = h.Image ?? string.Empty,
            Brand = h.BrandName,
            IsCityMatch = !string.IsNullOrWhiteSpace(normalized) &&
                          h.CityName.Contains(normalized, StringComparison.OrdinalIgnoreCase)
        }).ToList();

        var response = new PublicHotelSearchResponseDto
        {
            Type = list.Any(x => x.Name.Contains(q, StringComparison.OrdinalIgnoreCase)) ? "hotel" : "city",
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
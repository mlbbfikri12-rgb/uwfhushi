using Hotel.Api.Data;
using Hotel.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Api.Services;

public interface IPublicHomeService
{
    Task<PublicHomeResponseDto> GetHomeAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PublicBlogDto>> GetBlogsAsync(CancellationToken cancellationToken = default);
}

public class PublicHomeService : IPublicHomeService
{
    private readonly MasterDbContext _masterDb;
    private readonly IBannerService _bannerService;
    private readonly ICacheService _cache;

    public PublicHomeService(
        MasterDbContext masterDb,
        IBannerService bannerService,
        ICacheService cache)
    {
        _masterDb = masterDb;
        _bannerService = bannerService;
        _cache = cache;
    }

    public async Task<PublicHomeResponseDto> GetHomeAsync(CancellationToken cancellationToken = default)
    {
        // =========================
        // HERO
        // =========================
        var heroBanners = await _bannerService.GetActiveBannersAsync(cancellationToken);

        // =========================
        // POPULAR HOTELS
        // =========================
        var popularHotelsRaw = await _masterDb.Hotels
            .AsNoTracking()
            .Include(h => h.City)
            .Include(h => h.Brand)
            .Include(h => h.Images)
            .Where(h => h.IsActive)
            .OrderByDescending(h => h.Rating)
            .ThenByDescending(h => h.ReviewCount)
            .Take(6)
            .ToListAsync(cancellationToken);

        // 🔥 Ambil semua slug
        var slugs = popularHotelsRaw.Select(h => h.Slug).ToList();

        // 🔥 Ambil price dari master (O(1))
        var priceMap = await _masterDb.HotelPriceSummaries
            .Where(p => slugs.Contains(p.Slug))
            .ToDictionaryAsync(p => p.Slug, p => p.LowestPrice, cancellationToken);

        var popularHotels = popularHotelsRaw
            .Select(h => new PublicHotelListItemDto
            {
                HotelId = h.Id,
                Slug = h.Slug,
                BranchCode = h.BranchCode,
                Name = h.Name,
                City = h.City?.Name ?? string.Empty,
                Rating = h.Rating,
                PriceFrom = priceMap.TryGetValue(h.Slug, out var price) ? price : 0,
                Image = h.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault() ?? string.Empty,
                Brand = h.Brand?.Name ?? string.Empty,
                IsCityMatch = false
            })
            .ToList();

        // =========================
        // DESTINATIONS
        // =========================
        var cityHotels = await _masterDb.Hotels
            .AsNoTracking()
            .Include(h => h.City)
            .Where(h => h.IsActive && h.City != null)
            .Select(h => new
            {
                h.Slug,
                City = h.City!.Name
            })
            .ToListAsync(cancellationToken);

        var allSlugs = cityHotels.Select(x => x.Slug).Distinct().ToList();

        var allPrices = await _masterDb.HotelPriceSummaries
            .Where(p => allSlugs.Contains(p.Slug))
            .ToDictionaryAsync(p => p.Slug, p => p.LowestPrice, cancellationToken);

        var destinations = cityHotels
            .GroupBy(x => x.City)
            .Select(g =>
            {
                var minPrice = g
                    .Select(x => allPrices.TryGetValue(x.Slug, out var p) ? p : 0)
                    .Where(p => p > 0)
                    .DefaultIfEmpty(0)
                    .Min();

                return new PublicDestinationDto
                {
                    City = g.Key,
                    MinPrice = minPrice
                };
            })
            .OrderBy(x => x.City)
            .ToList();

        // =========================
        // BLOGS
        // =========================
        var blogs = await GetBlogsAsync(cancellationToken);

        return new PublicHomeResponseDto
        {
            HeroBanners = heroBanners,
            PopularHotels = popularHotels,
            Destinations = destinations,
            Blogs = blogs
        };
    }

    public async Task<IReadOnlyCollection<PublicBlogDto>> GetBlogsAsync(CancellationToken cancellationToken = default)
    {
        return await _masterDb.BlogPosts
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.CreatedAt)
            .Take(10)
            .Select(x => new PublicBlogDto
            {
                Id = x.Id,
                Title = x.Title,
                Content = x.Content,
                ImageUrl = x.ImageUrl,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
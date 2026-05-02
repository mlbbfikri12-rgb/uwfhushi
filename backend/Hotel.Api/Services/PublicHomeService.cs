using Hotel.Api.Data;
using Hotel.Api.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

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

    private const int MaxConcurrency = 5;
    private const int PriceCacheMinutes = 5;

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
        var popularHotels = await _masterDb.Hotels
            .AsNoTracking()
            .Include(h => h.City)
            .Include(h => h.Brand)
            .Include(h => h.Images)
            .Where(h => h.IsActive)
            .OrderByDescending(h => h.Rating)
            .ThenByDescending(h => h.ReviewCount)
            .Take(6)
            .Select(h => new PublicHotelListItemDto
            {
                HotelId = h.Id,
                Slug = h.Slug,
                BranchCode = h.BranchCode,
                Name = h.Name,
                City = h.City != null ? h.City.Name : string.Empty,
                Rating = h.Rating,
                PriceFrom = 0m,
                Image = h.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault() ?? string.Empty,
                Brand = h.Brand != null ? h.Brand.Name : string.Empty,
                IsCityMatch = false
            })
            .ToListAsync(cancellationToken);

        // =========================
        // COLLECT UNIQUE BRANCHES
        // =========================
        var branchCodes = popularHotels
            .Select(h => h.BranchCode)
            .Distinct()
            .ToList();

        // =========================
        // FETCH PRICES (PARALLEL + LIMITED + CACHE)
        // =========================
        var prices = await GetPricesBatchAsync(branchCodes, cancellationToken);

        foreach (var hotel in popularHotels)
        {
            if (prices.TryGetValue(hotel.BranchCode, out var price))
                hotel.PriceFrom = price;
        }

        // =========================
        // DESTINATIONS (NO N+1)
        // =========================
        var cityHotels = await _masterDb.Hotels
            .AsNoTracking()
            .Include(h => h.City)
            .Where(h => h.IsActive && h.City != null)
            .Select(h => new
            {
                h.BranchCode,
                City = h.City!.Name
            })
            .ToListAsync(cancellationToken);

        var allBranchCodes = cityHotels.Select(x => x.BranchCode).Distinct().ToList();
        var allPrices = await GetPricesBatchAsync(allBranchCodes, cancellationToken);

        var destinations = cityHotels
            .GroupBy(x => x.City)
            .Select(g =>
            {
                var minPrice = g
                    .Select(x => allPrices.TryGetValue(x.BranchCode, out var p) ? p : 0)
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

    // =========================
    // 🔥 CORE OPTIMIZATION
    // =========================
    private async Task<Dictionary<string, decimal>> GetPricesBatchAsync(
        List<string> branchCodes,
        CancellationToken cancellationToken)
    {
        var result = new ConcurrentDictionary<string, decimal>();
        var semaphore = new SemaphoreSlim(MaxConcurrency);

        var tasks = branchCodes.Select(async code =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var price = await GetMinPriceWithCacheAsync(code, cancellationToken);
                result[code] = price;
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        return result.ToDictionary(x => x.Key, x => x.Value);
    }

    private async Task<decimal> GetMinPriceWithCacheAsync(
        string branchCode,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"price:{branchCode}";

        var cached = await _cache.GetAsync<decimal?>(cacheKey, cancellationToken);
        if (cached.HasValue)
            return cached.Value;

        var price = await GetMinPriceForBranchAsync(branchCode, cancellationToken);

        await _cache.SetAsync(
            cacheKey,
            price,
            TimeSpan.FromMinutes(PriceCacheMinutes),
            cancellationToken);

        return price;
    }

    private async Task<decimal> GetMinPriceForBranchAsync(
        string branchCode,
        CancellationToken cancellationToken)
    {
        var branch = await _masterDb.Branches
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == branchCode && x.IsActive, cancellationToken);

        if (branch == null) return 0m;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql($"Host={branch.DbHost};Port={branch.DbPort};Database={branch.DbName};Username={branch.DbUser};Password={branch.DbPassword}")
            .Options;

        await using var tenantDb = new AppDbContext(options);

        var minRatePlan = await tenantDb.RatePlans
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => (decimal?)x.Price)
            .MinAsync(cancellationToken);

        var minBasePrice = await tenantDb.RoomTypes
            .AsNoTracking()
            .Select(x => (decimal?)x.BasePrice)
            .MinAsync(cancellationToken);

        return minRatePlan ?? minBasePrice ?? 0m;
    }
}
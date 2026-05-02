using Hotel.Api.Configurations;
using Hotel.Api.Data;
using Hotel.Api.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Hotel.Api.Services;

public interface IPublicBranchService
{
    Task<IReadOnlyCollection<PublicSearchResultDto>> SearchBranchesAsync(
        string? q,
        int limit,
        CancellationToken cancellationToken = default);
}

public class PublicBranchService : IPublicBranchService
{
    private const double TrigramThreshold = 0.2d;

    private readonly IDbContextFactory<MasterDbContext> _factory;
    private readonly ICacheService _cache;
    private readonly CacheSettings _cacheSettings;

    public PublicBranchService(
        IDbContextFactory<MasterDbContext> factory,
        ICacheService cache,
        IOptions<CacheSettings> cacheSettings)
    {
        _factory = factory;
        _cache = cache;
        _cacheSettings = cacheSettings.Value;
    }

    public async Task<IReadOnlyCollection<PublicSearchResultDto>> SearchBranchesAsync(
        string? q,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var keyword = q?.Trim();
        if (string.IsNullOrWhiteSpace(keyword))
            return Array.Empty<PublicSearchResultDto>();

        if (keyword.Length < 2)
            return Array.Empty<PublicSearchResultDto>();

        if (limit < 1) limit = 1;
        if (limit > 20) limit = 20;

        var normalized = keyword.ToLowerInvariant();
        var cacheKey = $"public:search:v3:{normalized}:limit:{limit}";

        var cached = await _cache.GetAsync<IReadOnlyCollection<PublicSearchResultDto>>(cacheKey, cancellationToken);
        if (cached != null)
            return cached;

        await using var db1 = await _factory.CreateDbContextAsync(cancellationToken);
        await using var db2 = await _factory.CreateDbContextAsync(cancellationToken);

        db1.Database.SetCommandTimeout(3);
        db2.Database.SetCommandTimeout(3);

        // =========================
        // 🔥 FAST PATH (PREFIX)
        // =========================

        var fastCityTask = db1.Cities
            .AsNoTracking()
            .Where(c => EF.Functions.ILike(c.Name, $"{normalized}%"))
            .Select(c => new PublicSearchResultDto
            {
                Type = "city",
                Name = c.Name,
                Code = string.Empty,
                Score = 300d
            })
            .Take(limit)
            .ToListAsync(cancellationToken);

        var fastHotelTask = db2.Hotels
            .AsNoTracking()
            .Where(h => h.IsActive)
            .Where(h => EF.Functions.ILike(h.Name, $"{normalized}%"))
            .Select(h => new PublicSearchResultDto
            {
                Type = "hotel",
                Name = h.Name,
                Code = h.BranchCode,
                Score = 200d
            })
            .Take(limit)
            .ToListAsync(cancellationToken);

        await Task.WhenAll(fastCityTask, fastHotelTask);

        var fastResults = fastCityTask.Result
            .Concat(fastHotelTask.Result)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Name)
            .Take(limit)
            .ToList();

        // =========================
        // 🔥 FALLBACK (FUZZY)
        // =========================

        if (fastResults.Count < limit)
        {
            var cityTask = db1.Cities
                .AsNoTracking()
                .Where(c =>
                    EF.Functions.ILike(c.Name, $"%{normalized}%") ||
                    EF.Functions.TrigramsSimilarity(c.Name, normalized) > TrigramThreshold)
                .Select(c => new PublicSearchResultDto
                {
                    Type = "city",
                    Name = c.Name,
                    Code = string.Empty,
                    Score =
                        (EF.Functions.ILike(c.Name, $"{normalized}%") ? 250d : 0d) +
                        (EF.Functions.ILike(c.Name, $"%{normalized}%") ? 120d : 0d) +
                        (EF.Functions.TrigramsSimilarity(c.Name, normalized) * 100d) +
                        1000d // 🔥 boost city biar selalu di atas
                })
                .Take(limit)
                .ToListAsync(cancellationToken);

            var hotelTask = db2.Hotels
                .AsNoTracking()
                .Where(h => h.IsActive)
                .Where(h =>
                    EF.Functions.ILike(h.Name, $"%{normalized}%") ||
                    EF.Functions.ILike(h.BranchCode, $"%{normalized}%") ||
                    EF.Functions.TrigramsSimilarity(h.Name, normalized) > TrigramThreshold)
                .Select(h => new PublicSearchResultDto
                {
                    Type = "hotel",
                    Name = h.Name,
                    Code = h.BranchCode,
                    Score =
                        (EF.Functions.ILike(h.Name, $"{normalized}%") ? 220d : 0d) +
                        (EF.Functions.ILike(h.Name, $"%{normalized}%") ? 110d : 0d) +
                        (EF.Functions.TrigramsSimilarity(h.Name, normalized) * 90d)
                })
                .Take(limit)
                .ToListAsync(cancellationToken);

            await Task.WhenAll(cityTask, hotelTask);

            fastResults = fastResults
                .Concat(cityTask.Result)
                .Concat(hotelTask.Result)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Type)
                .ThenBy(x => x.Name)
                .DistinctBy(x => new { x.Type, x.Name })
                .Take(limit)
                .ToList();
        }

        await _cache.SetAsync(
            cacheKey,
            fastResults,
            TimeSpan.FromMinutes(_cacheSettings.BranchSearchTtlMinutes),
            cancellationToken);

        return fastResults;
    }
}
using Hotel.Api.Configurations;
using Hotel.Api.Data;
using Hotel.Api.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Hotel.Api.Services;

public interface IPublicBranchService
{
    Task<IReadOnlyCollection<PublicBranchDto>> SearchBranchesAsync(string? q, int limit, CancellationToken cancellationToken = default);
}

public class PublicBranchService : IPublicBranchService
{
    private readonly MasterDbContext _masterDb;
    private readonly ICacheService _cache;
    private readonly CacheSettings _cacheSettings;

    public PublicBranchService(
        MasterDbContext masterDb,
        ICacheService cache,
        IOptions<CacheSettings> cacheSettings)
    {
        _masterDb = masterDb;
        _cache = cache;
        _cacheSettings = cacheSettings.Value;
    }

    public async Task<IReadOnlyCollection<PublicBranchDto>> SearchBranchesAsync(
        string? q,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (limit < 1) limit = 1;
        if (limit > 50) limit = 50;

        var keyword = q?.Trim().ToUpperInvariant() ?? string.Empty;
        var cacheKey = $"branches:search:{keyword}:limit:{limit}";
        var cached = await _cache.GetAsync<IReadOnlyCollection<PublicBranchDto>>(cacheKey, cancellationToken);
        if (cached != null)
            return cached;

        var query = _masterDb.Branches
            .AsNoTracking()
            .Where(b => b.IsActive);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(b =>
                EF.Functions.ILike(b.Code, $"%{keyword}%") ||
                EF.Functions.ILike(b.Name, $"%{keyword}%"));
        }

        var branches = await query
            .OrderBy(b => b.Code)
            .Take(limit)
            .Select(b => new PublicBranchDto
            {
                Id = b.Id,
                Name = b.Name,
                Code = b.Code
            })
            .ToListAsync(cancellationToken);

        await _cache.SetAsync(
            cacheKey,
            branches,
            TimeSpan.FromMinutes(_cacheSettings.BranchSearchTtlMinutes),
            cancellationToken);

        return branches;
    }
}

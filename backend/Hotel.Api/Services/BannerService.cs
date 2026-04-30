using Hotel.Api.Data;
using Hotel.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Api.Services;

public interface IBannerService
{
    Task<IReadOnlyCollection<HeroBannerResponseDto>> GetActiveBannersAsync(CancellationToken cancellationToken = default);
}

public class BannerService : IBannerService
{
    private readonly MasterDbContext _masterDb;

    public BannerService(MasterDbContext masterDb)
    {
        _masterDb = masterDb;
    }

    public async Task<IReadOnlyCollection<HeroBannerResponseDto>> GetActiveBannersAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _masterDb.HeroBanners
            .AsNoTracking()
            .Where(b =>
                b.IsActive &&
                (b.StartsAt == null || b.StartsAt <= now) &&
                (b.EndsAt == null || b.EndsAt >= now))
            .OrderBy(b => b.SortOrder)
            .ThenByDescending(b => b.CreatedAt)
            .Select(b => new HeroBannerResponseDto
            {
                Id = b.Id,
                Title = b.Title,
                Subtitle = b.Subtitle,
                ImageUrl = b.ImageUrl,
                LinkUrl = b.LinkUrl,
                SortOrder = b.SortOrder
            })
            .ToListAsync(cancellationToken);
    }
}

using Hotel.Api.Data;
using Hotel.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Api.Services;

public interface IBannerService
{
    Task<IReadOnlyCollection<HeroBannerResponseDto>> GetActiveBannersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<HeroBannerResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<HeroBannerResponseDto> CreateAsync(UpsertHeroBannerDto dto, CancellationToken cancellationToken = default);
    Task<HeroBannerResponseDto?> UpdateAsync(Guid id, UpsertHeroBannerDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}


public class BannerService : IBannerService
{
    private readonly MasterDbContext _db;

    public BannerService(MasterDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyCollection<HeroBannerResponseDto>> GetActiveBannersAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _db.HeroBanners
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

    public async Task<IReadOnlyCollection<HeroBannerResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.HeroBanners
            .AsNoTracking()
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

    public async Task<HeroBannerResponseDto> CreateAsync(UpsertHeroBannerDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new ArgumentException("Title is required");

        if (string.IsNullOrWhiteSpace(dto.ImageUrl))
            throw new ArgumentException("Image URL is required");

        var entity = new Entities.Master.HeroBanner
        {
            Id = Guid.NewGuid(),
            Title = dto.Title.Trim(),
            Subtitle = dto.Subtitle.Trim(),
            LinkUrl = dto.LinkUrl.Trim(),
            ImageUrl = dto.ImageUrl.Trim(),
            SortOrder = dto.SortOrder,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.HeroBanners.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<HeroBannerResponseDto?> UpdateAsync(Guid id, UpsertHeroBannerDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _db.HeroBanners.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) return null;

        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new ArgumentException("Title is required");

        if (string.IsNullOrWhiteSpace(dto.ImageUrl))
            throw new ArgumentException("Image URL is required");

        entity.Title = dto.Title.Trim();
        entity.Subtitle = dto.Subtitle.Trim();
        entity.LinkUrl = dto.LinkUrl.Trim();
        entity.ImageUrl = dto.ImageUrl.Trim();
        entity.SortOrder = dto.SortOrder;
        entity.IsActive = dto.IsActive;

        await _db.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.HeroBanners.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) return false;

        _db.HeroBanners.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static HeroBannerResponseDto Map(Entities.Master.HeroBanner b)
    {
        return new HeroBannerResponseDto
        {
            Id = b.Id,
            Title = b.Title,
            Subtitle = b.Subtitle,
            ImageUrl = b.ImageUrl,
            LinkUrl = b.LinkUrl,
            SortOrder = b.SortOrder
        };
    }
}
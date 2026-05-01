using Hotel.Api.Data;
using Hotel.Api.DTOs;
using Hotel.Api.Entities.Master;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Api.Services;

public interface IBrandService
{
    Task<IReadOnlyCollection<BrandResponseDto>> GetAsync(string? q, CancellationToken cancellationToken = default);
    Task<BrandResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BrandResponseDto> CreateAsync(BrandUpsertDto dto, CancellationToken cancellationToken = default);
    Task<BrandResponseDto?> UpdateAsync(Guid id, BrandUpsertDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public class BrandService : IBrandService
{
    private readonly MasterDbContext _db;

    public BrandService(MasterDbContext db) => _db = db;

    public async Task<IReadOnlyCollection<BrandResponseDto>> GetAsync(string? q, CancellationToken cancellationToken = default)
    {
        var query = _db.Brands.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            query = query.Where(x => EF.Functions.ILike(x.Name, $"%{keyword}%"));
        }

        return await query
            .OrderBy(x => x.Name)
            .Select(x => new BrandResponseDto { Id = x.Id, Name = x.Name, LogoUrl = x.LogoUrl, IsActive = x.IsActive })
            .ToListAsync(cancellationToken);
    }

    public async Task<BrandResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Brands.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new BrandResponseDto { Id = x.Id, Name = x.Name, LogoUrl = x.LogoUrl, IsActive = x.IsActive })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<BrandResponseDto> CreateAsync(BrandUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new Exception("Brand name is required");

        var exists = await _db.Brands.AnyAsync(x => x.IsActive && x.Name.ToLower() == name.ToLower(), cancellationToken);
        if (exists)
            throw new Exception("Brand already exists");

        var entity = new Brand { Id = Guid.NewGuid(), Name = name, LogoUrl = dto.LogoUrl?.Trim(), IsActive = true };
        _db.Brands.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return new BrandResponseDto { Id = entity.Id, Name = entity.Name, LogoUrl = entity.LogoUrl, IsActive = entity.IsActive };
    }

    public async Task<BrandResponseDto?> UpdateAsync(Guid id, BrandUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var brand = await _db.Brands.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (brand == null) return null;

        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new Exception("Brand name is required");

        var duplicate = await _db.Brands.AnyAsync(x => x.Id != id && x.IsActive && x.Name.ToLower() == name.ToLower(), cancellationToken);
        if (duplicate)
            throw new Exception("Brand already exists");

        brand.Name = name;
        brand.LogoUrl = dto.LogoUrl?.Trim();
        await _db.SaveChangesAsync(cancellationToken);
        return new BrandResponseDto { Id = brand.Id, Name = brand.Name, LogoUrl = brand.LogoUrl, IsActive = brand.IsActive };
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var brand = await _db.Brands.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (brand == null) return false;
        brand.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

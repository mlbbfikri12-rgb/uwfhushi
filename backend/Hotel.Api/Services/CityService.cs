using Hotel.Api.Data;
using Hotel.Api.DTOs;
using Hotel.Api.Entities.Master;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Api.Services;

public interface ICityService
{
    Task<IReadOnlyCollection<CityResponseDto>> GetAsync(string? q, CancellationToken cancellationToken = default);
    Task<CityResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CityResponseDto> CreateAsync(CityUpsertDto dto, CancellationToken cancellationToken = default);
    Task<CityResponseDto?> UpdateAsync(Guid id, CityUpsertDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public class CityService : ICityService
{
    private readonly MasterDbContext _db;

    public CityService(MasterDbContext db) => _db = db;

    public async Task<IReadOnlyCollection<CityResponseDto>> GetAsync(string? q, CancellationToken cancellationToken = default)
    {
        var query = _db.Cities.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            query = query.Where(x => EF.Functions.ILike(x.Name, $"%{keyword}%"));
        }

        return await query
            .OrderBy(x => x.Name)
            .Select(x => new CityResponseDto { Id = x.Id, Name = x.Name, IsActive = x.IsActive })
            .ToListAsync(cancellationToken);
    }

    public async Task<CityResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Cities.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new CityResponseDto { Id = x.Id, Name = x.Name, IsActive = x.IsActive })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CityResponseDto> CreateAsync(CityUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new Exception("City name is required");

        var exists = await _db.Cities.AnyAsync(x => x.IsActive && x.Name.ToLower() == name.ToLower(), cancellationToken);
        if (exists)
            throw new Exception("City already exists");

        var entity = new City { Id = Guid.NewGuid(), Name = name, IsActive = true };
        _db.Cities.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return new CityResponseDto { Id = entity.Id, Name = entity.Name, IsActive = entity.IsActive };
    }

    public async Task<CityResponseDto?> UpdateAsync(Guid id, CityUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var city = await _db.Cities.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (city == null) return null;

        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new Exception("City name is required");

        var duplicate = await _db.Cities.AnyAsync(x => x.Id != id && x.IsActive && x.Name.ToLower() == name.ToLower(), cancellationToken);
        if (duplicate)
            throw new Exception("City already exists");

        city.Name = name;
        await _db.SaveChangesAsync(cancellationToken);
        return new CityResponseDto { Id = city.Id, Name = city.Name, IsActive = city.IsActive };
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var city = await _db.Cities.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (city == null) return false;
        city.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

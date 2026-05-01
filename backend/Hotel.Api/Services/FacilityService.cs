using Hotel.Api.Data;
using Hotel.Api.DTOs;
using Hotel.Api.Entities.Master;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Api.Services;

public interface IFacilityService
{
    Task<IReadOnlyCollection<FacilityResponseDto>> GetAsync(string? q, CancellationToken cancellationToken = default);
    Task<FacilityResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FacilityResponseDto> CreateAsync(FacilityUpsertDto dto, CancellationToken cancellationToken = default);
    Task<FacilityResponseDto?> UpdateAsync(Guid id, FacilityUpsertDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public class FacilityService : IFacilityService
{
    private readonly MasterDbContext _db;

    public FacilityService(MasterDbContext db) => _db = db;

    public async Task<IReadOnlyCollection<FacilityResponseDto>> GetAsync(string? q, CancellationToken cancellationToken = default)
    {
        var query = _db.Facilities.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            query = query.Where(x => EF.Functions.ILike(x.Name, $"%{keyword}%"));
        }

        return await query.OrderBy(x => x.Name)
            .Select(x => new FacilityResponseDto { Id = x.Id, Name = x.Name, Icon = x.Icon, IsActive = x.IsActive })
            .ToListAsync(cancellationToken);
    }

    public async Task<FacilityResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Facilities.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new FacilityResponseDto { Id = x.Id, Name = x.Name, Icon = x.Icon, IsActive = x.IsActive })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<FacilityResponseDto> CreateAsync(FacilityUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new Exception("Facility name is required");

        var exists = await _db.Facilities.AnyAsync(x => x.IsActive && x.Name.ToLower() == name.ToLower(), cancellationToken);
        if (exists)
            throw new Exception("Facility already exists");

        var entity = new Facility { Id = Guid.NewGuid(), Name = name, Icon = dto.Icon?.Trim() ?? string.Empty, IsActive = true };
        _db.Facilities.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return new FacilityResponseDto { Id = entity.Id, Name = entity.Name, Icon = entity.Icon, IsActive = entity.IsActive };
    }

    public async Task<FacilityResponseDto?> UpdateAsync(Guid id, FacilityUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var facility = await _db.Facilities.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (facility == null) return null;

        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new Exception("Facility name is required");

        var duplicate = await _db.Facilities.AnyAsync(x => x.Id != id && x.IsActive && x.Name.ToLower() == name.ToLower(), cancellationToken);
        if (duplicate)
            throw new Exception("Facility already exists");

        facility.Name = name;
        facility.Icon = dto.Icon?.Trim() ?? string.Empty;
        await _db.SaveChangesAsync(cancellationToken);
        return new FacilityResponseDto { Id = facility.Id, Name = facility.Name, Icon = facility.Icon, IsActive = facility.IsActive };
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var facility = await _db.Facilities.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (facility == null) return false;
        facility.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

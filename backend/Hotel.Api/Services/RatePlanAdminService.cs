using Hotel.Api.Data;
using Hotel.Api.DTOs;
using Hotel.Api.Entities.Tenant;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Api.Services;

public interface IRatePlanAdminService
{
    Task<IReadOnlyCollection<RatePlanAdminDto>> GetByRoomTypeAsync(Guid roomTypeId, CancellationToken cancellationToken = default);
    Task<RatePlanAdminDto> CreateAsync(Guid roomTypeId, UpsertRatePlanDto dto, CancellationToken cancellationToken = default);
    Task<RatePlanAdminDto?> UpdateAsync(Guid id, UpsertRatePlanDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public class RatePlanAdminService : IRatePlanAdminService
{
    private readonly AppDbContext _db;

    public RatePlanAdminService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyCollection<RatePlanAdminDto>> GetByRoomTypeAsync(Guid roomTypeId, CancellationToken cancellationToken = default)
    {
        var exists = await _db.RoomTypes.AnyAsync(x => x.Id == roomTypeId, cancellationToken);
        if (!exists) throw new Exception("Room type not found");

        return await _db.RatePlans
            .AsNoTracking()
            .Where(x => x.RoomTypeId == roomTypeId)
            .OrderBy(x => x.Price)
            .Select(x => new RatePlanAdminDto
            {
                Id = x.Id,
                RoomTypeId = x.RoomTypeId,
                Name = x.Name,
                Price = x.Price,
                IncludesBreakfast = x.IncludesBreakfast,
                IsRefundable = x.IsRefundable,
                PaymentType = x.PaymentType,
                TermsConditions = x.TermsConditions,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<RatePlanAdminDto> CreateAsync(Guid roomTypeId, UpsertRatePlanDto dto, CancellationToken cancellationToken = default)
    {
        await ValidateInputAsync(roomTypeId, dto, null, cancellationToken);

        var entity = new RatePlan
        {
            Id = Guid.NewGuid(),
            RoomTypeId = roomTypeId,
            Name = dto.Name.Trim(),
            Price = dto.Price,
            IncludesBreakfast = dto.IncludesBreakfast,
            IsRefundable = dto.IsRefundable,
            PaymentType = dto.PaymentType.Trim().ToLowerInvariant(),
            TermsConditions = dto.TermsConditions.Trim(),
            IsActive = dto.IsActive
        };

        _db.RatePlans.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<RatePlanAdminDto?> UpdateAsync(Guid id, UpsertRatePlanDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _db.RatePlans.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) return null;

        await ValidateInputAsync(entity.RoomTypeId, dto, id, cancellationToken);

        entity.Name = dto.Name.Trim();
        entity.Price = dto.Price;
        entity.IncludesBreakfast = dto.IncludesBreakfast;
        entity.IsRefundable = dto.IsRefundable;
        entity.PaymentType = dto.PaymentType.Trim().ToLowerInvariant();
        entity.TermsConditions = dto.TermsConditions.Trim();
        entity.IsActive = dto.IsActive;

        await _db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.RatePlans.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) return false;
        _db.RatePlans.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task ValidateInputAsync(Guid roomTypeId, UpsertRatePlanDto dto, Guid? id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new Exception("Rate plan name is required");
        if (dto.Price <= 0)
            throw new Exception("Rate plan price must be greater than 0");

        var roomTypeExists = await _db.RoomTypes.AnyAsync(x => x.Id == roomTypeId, cancellationToken);
        if (!roomTypeExists)
            throw new Exception("Room type not found");

        var paymentType = dto.PaymentType.Trim().ToLowerInvariant();
        if (paymentType != "online" && paymentType != "pay_at_hotel")
            throw new Exception("Payment type must be online or pay_at_hotel");

        var duplicate = await _db.RatePlans.AnyAsync(
            x => x.Id != id &&
                 x.RoomTypeId == roomTypeId &&
                 x.Name.ToLower() == dto.Name.Trim().ToLower(),
            cancellationToken);

        if (duplicate)
            throw new Exception("Rate plan name already exists for this room type");
    }

    private static RatePlanAdminDto Map(RatePlan entity)
    {
        return new RatePlanAdminDto
        {
            Id = entity.Id,
            RoomTypeId = entity.RoomTypeId,
            Name = entity.Name,
            Price = entity.Price,
            IncludesBreakfast = entity.IncludesBreakfast,
            IsRefundable = entity.IsRefundable,
            PaymentType = entity.PaymentType,
            TermsConditions = entity.TermsConditions,
            IsActive = entity.IsActive
        };
    }
}

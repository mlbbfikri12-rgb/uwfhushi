using Hotel.Api.Data;
using Hotel.Api.DTOs;
using Hotel.Api.Entities.Master;
using Microsoft.EntityFrameworkCore;
using MasterHotel = Hotel.Api.Entities.Master.Hotel;

namespace Hotel.Api.Services;

public interface IHotelAdminService
{
    Task<IReadOnlyCollection<HotelResponseDto>> GetAsync(string? q, CancellationToken cancellationToken = default);
    Task<HotelResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<HotelResponseDto> CreateAsync(HotelUpsertDto dto, CancellationToken cancellationToken = default);
    Task<HotelResponseDto?> UpdateAsync(Guid id, HotelUpsertDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<HotelImageAdminDto> AddImageAsync(Guid hotelId, AddHotelImageDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteImageAsync(Guid hotelId, Guid imageId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<HotelFacilityAdminDto>> SetFacilitiesAsync(Guid hotelId, Guid[] facilityIds, CancellationToken cancellationToken = default);
    Task<NearbyPlaceAdminDto> AddNearbyPlaceAsync(Guid hotelId, AddNearbyPlaceDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteNearbyPlaceAsync(Guid hotelId, Guid nearbyPlaceId, CancellationToken cancellationToken = default);
}

public class HotelAdminService : IHotelAdminService
{
    private readonly MasterDbContext _db;
    private readonly IHotelPriceSummaryUpdater _priceSummaryUpdater;
    private readonly ILogger<HotelAdminService> _logger;

    public HotelAdminService(
        MasterDbContext db,
        IHotelPriceSummaryUpdater priceSummaryUpdater,
        ILogger<HotelAdminService> logger)
    {
        _db = db;
        _priceSummaryUpdater = priceSummaryUpdater;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<HotelResponseDto>> GetAsync(string? q, CancellationToken cancellationToken = default)
    {
        var query = _db.Hotels
            .AsNoTracking()
            .Include(h => h.City)
            .Include(h => h.Brand)
            .Where(h => h.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            query = query.Where(h =>
                EF.Functions.ILike(h.Name, $"%{keyword}%") ||
                EF.Functions.ILike(h.BranchCode, $"%{keyword}%") ||
                EF.Functions.ILike(h.Slug, $"%{keyword}%"));
        }

        var hotels = await query.OrderBy(h => h.Name).ToListAsync(cancellationToken);
        return hotels.Select(MapHotel).ToList();
    }

    public async Task<HotelResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var hotel = await _db.Hotels
            .AsNoTracking()
            .Include(h => h.City)
            .Include(h => h.Brand)
            .Include(h => h.Images)
            .Include(h => h.HotelFacilities)
            .ThenInclude(x => x.Facility)
            .Include(h => h.NearbyPlaces)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        return hotel == null ? null : MapHotel(hotel);
    }

    public async Task<HotelResponseDto> CreateAsync(HotelUpsertDto dto, CancellationToken cancellationToken = default)
    {
        await ValidateHotelInputAsync(dto, null, cancellationToken);

        var entity = new MasterHotel
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Slug = dto.Slug.Trim().ToLowerInvariant(),
            BranchCode = dto.BranchCode.Trim().ToUpperInvariant(),
            CityId = dto.CityId,
            BrandId = dto.BrandId,
            Address = dto.Address.Trim(),
            Description = dto.Description.Trim(),
            StarRating = dto.StarRating,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Rating = dto.StarRating,
            ReviewCount = 0,
            IsActive = dto.IsActive
        };

        _db.Hotels.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        await EnqueuePriceSummaryUpdateAsync(entity.BranchCode, cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<HotelResponseDto?> UpdateAsync(Guid id, HotelUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var hotel = await _db.Hotels.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (hotel == null) return null;

        await ValidateHotelInputAsync(dto, id, cancellationToken);

        hotel.Name = dto.Name.Trim();
        hotel.Slug = dto.Slug.Trim().ToLowerInvariant();
        hotel.BranchCode = dto.BranchCode.Trim().ToUpperInvariant();
        hotel.CityId = dto.CityId;
        hotel.BrandId = dto.BrandId;
        hotel.Address = dto.Address.Trim();
        hotel.Description = dto.Description.Trim();
        hotel.StarRating = dto.StarRating;
        hotel.Latitude = dto.Latitude;
        hotel.Longitude = dto.Longitude;
        hotel.IsActive = dto.IsActive;

        await _db.SaveChangesAsync(cancellationToken);
        await EnqueuePriceSummaryUpdateAsync(hotel.BranchCode, cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var hotel = await _db.Hotels.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (hotel == null) return false;
        hotel.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);
        await EnqueuePriceSummaryUpdateAsync(hotel.BranchCode, cancellationToken);
        return true;
    }

    public async Task<HotelImageAdminDto> AddImageAsync(Guid hotelId, AddHotelImageDto dto, CancellationToken cancellationToken = default)
    {
        var hotel = await _db.Hotels.FirstOrDefaultAsync(x => x.Id == hotelId && x.IsActive, cancellationToken);
        if (hotel == null) throw new Exception("Hotel not found");
        if (string.IsNullOrWhiteSpace(dto.Url)) throw new Exception("Image URL is required");

        if (dto.IsPrimary)
        {
            var existingPrimary = await _db.HotelImages.Where(x => x.HotelId == hotelId && x.IsPrimary).ToListAsync(cancellationToken);
            foreach (var image in existingPrimary) image.IsPrimary = false;
        }

        var imageEntity = new HotelImage
        {
            Id = Guid.NewGuid(),
            HotelId = hotelId,
            Url = dto.Url.Trim(),
            IsPrimary = dto.IsPrimary,
            SortOrder = dto.SortOrder,
            Type = "hotel"
        };

        _db.HotelImages.Add(imageEntity);
        await _db.SaveChangesAsync(cancellationToken);

        return new HotelImageAdminDto
        {
            Id = imageEntity.Id,
            Url = imageEntity.Url,
            IsPrimary = imageEntity.IsPrimary,
            SortOrder = imageEntity.SortOrder
        };
    }

    public async Task<bool> DeleteImageAsync(Guid hotelId, Guid imageId, CancellationToken cancellationToken = default)
    {
        var image = await _db.HotelImages.FirstOrDefaultAsync(x => x.HotelId == hotelId && x.Id == imageId, cancellationToken);
        if (image == null) return false;
        _db.HotelImages.Remove(image);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyCollection<HotelFacilityAdminDto>> SetFacilitiesAsync(Guid hotelId, Guid[] facilityIds, CancellationToken cancellationToken = default)
    {
        var hotel = await _db.Hotels.FirstOrDefaultAsync(x => x.Id == hotelId && x.IsActive, cancellationToken);
        if (hotel == null) throw new Exception("Hotel not found");

        var activeFacilities = await _db.Facilities
            .Where(x => x.IsActive && facilityIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Name, x.Icon })
            .ToListAsync(cancellationToken);

        var existing = await _db.HotelFacilities.Where(x => x.HotelId == hotelId).ToListAsync(cancellationToken);
        _db.HotelFacilities.RemoveRange(existing);

        var newLinks = activeFacilities.Select(x => new HotelFacility { HotelId = hotelId, FacilityId = x.Id }).ToList();
        _db.HotelFacilities.AddRange(newLinks);
        await _db.SaveChangesAsync(cancellationToken);

        return activeFacilities.Select(x => new HotelFacilityAdminDto { FacilityId = x.Id, Name = x.Name, Icon = x.Icon }).ToList();
    }

    public async Task<NearbyPlaceAdminDto> AddNearbyPlaceAsync(Guid hotelId, AddNearbyPlaceDto dto, CancellationToken cancellationToken = default)
    {
        var hotel = await _db.Hotels.FirstOrDefaultAsync(x => x.Id == hotelId && x.IsActive, cancellationToken);
        if (hotel == null) throw new Exception("Hotel not found");
        if (string.IsNullOrWhiteSpace(dto.Name)) throw new Exception("Nearby place name is required");
        if (string.IsNullOrWhiteSpace(dto.Distance)) throw new Exception("Nearby place distance is required");

        var entity = new NearbyPlace
        {
            Id = Guid.NewGuid(),
            HotelId = hotelId,
            Name = dto.Name.Trim(),
            Distance = dto.Distance.Trim(),
            DistanceKm = ParseDistanceKm(dto.Distance)
        };
        _db.NearbyPlaces.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return new NearbyPlaceAdminDto { Id = entity.Id, Name = entity.Name, Distance = entity.Distance };
    }

    public async Task<bool> DeleteNearbyPlaceAsync(Guid hotelId, Guid nearbyPlaceId, CancellationToken cancellationToken = default)
    {
        var entity = await _db.NearbyPlaces.FirstOrDefaultAsync(x => x.HotelId == hotelId && x.Id == nearbyPlaceId, cancellationToken);
        if (entity == null) return false;
        _db.NearbyPlaces.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task ValidateHotelInputAsync(HotelUpsertDto dto, Guid? hotelId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) throw new Exception("Hotel name is required");
        if (string.IsNullOrWhiteSpace(dto.Slug)) throw new Exception("Slug is required");
        if (string.IsNullOrWhiteSpace(dto.BranchCode)) throw new Exception("Branch code is required");
        if (dto.StarRating < 1 || dto.StarRating > 5) throw new Exception("Star rating must be between 1 and 5");

        var cityExists = await _db.Cities.AnyAsync(x => x.Id == dto.CityId && x.IsActive, cancellationToken);
        if (!cityExists) throw new Exception("City not found");

        if (dto.BrandId.HasValue)
        {
            var brandExists = await _db.Brands.AnyAsync(x => x.Id == dto.BrandId.Value && x.IsActive, cancellationToken);
            if (!brandExists) throw new Exception("Brand not found");
        }

        var normalizedBranchCode = dto.BranchCode.Trim().ToUpperInvariant();
        var branchExists = await _db.Branches.AnyAsync(x => x.Code == normalizedBranchCode && x.IsActive, cancellationToken);
        if (!branchExists) throw new Exception("Branch code not found in branch master");

        var branchDuplicate = await _db.Hotels.AnyAsync(
            x => x.Id != hotelId && x.BranchCode == normalizedBranchCode && x.IsActive,
            cancellationToken);
        if (branchDuplicate) throw new Exception("Branch code already used by another hotel");

        var normalizedSlug = dto.Slug.Trim().ToLowerInvariant();
        var slugDuplicate = await _db.Hotels.AnyAsync(
            x => x.Id != hotelId && x.Slug == normalizedSlug && x.IsActive,
            cancellationToken);
        if (slugDuplicate) throw new Exception("Slug already exists");
    }

    private static decimal ParseDistanceKm(string distance)
    {
        var parts = distance.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return 0m;
        return decimal.TryParse(parts[0].Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? value
            : 0m;
    }

    private static HotelResponseDto MapHotel(MasterHotel hotel)
    {
        return new HotelResponseDto
        {
            Id = hotel.Id,
            Name = hotel.Name,
            Slug = hotel.Slug,
            BranchCode = hotel.BranchCode,
            CityId = hotel.CityId,
            CityName = hotel.City?.Name ?? string.Empty,
            BrandId = hotel.BrandId,
            BrandName = hotel.Brand?.Name,
            Address = hotel.Address,
            Description = hotel.Description,
            StarRating = hotel.StarRating,
            Latitude = hotel.Latitude,
            Longitude = hotel.Longitude,
            IsActive = hotel.IsActive,
            Images = hotel.Images
                .OrderBy(x => x.SortOrder)
                .Select(x => new HotelImageAdminDto
                {
                    Id = x.Id,
                    Url = x.Url,
                    IsPrimary = x.IsPrimary,
                    SortOrder = x.SortOrder
                }).ToList(),
            Facilities = hotel.HotelFacilities
                .Where(x => x.Facility != null)
                .Select(x => new HotelFacilityAdminDto
                {
                    FacilityId = x.FacilityId,
                    Name = x.Facility!.Name,
                    Icon = x.Facility.Icon
                }).ToList(),
            NearbyPlaces = hotel.NearbyPlaces
                .OrderBy(x => x.DistanceKm)
                .Select(x => new NearbyPlaceAdminDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Distance = string.IsNullOrWhiteSpace(x.Distance) ? $"{x.DistanceKm:0.##} km" : x.Distance
                }).ToList()
        };
    }

    private async Task EnqueuePriceSummaryUpdateAsync(string branchCode, CancellationToken cancellationToken)
    {
        await _priceSummaryUpdater.EnqueueBranchAsync(branchCode, cancellationToken);
        _logger.LogInformation("Queued hotel price summary update after hotel mutation. BranchCode={BranchCode}", branchCode);
    }
}

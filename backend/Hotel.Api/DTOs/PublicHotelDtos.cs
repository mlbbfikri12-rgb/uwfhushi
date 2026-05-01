namespace Hotel.Api.DTOs;

public class PublicHotelSearchQueryDto
{
    public string? Q { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int TotalRooms { get; set; } = 1;
    public Guid? CityId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int[]? Stars { get; set; }
    public Guid[]? Brands { get; set; }
    public string[]? BrandNames { get; set; }
}

public class PublicHotelSearchResponseDto
{
    public string Type { get; set; } = "city";
    public IReadOnlyCollection<PublicHotelListItemDto> Hotels { get; set; } = Array.Empty<PublicHotelListItemDto>();
}

public class PublicHotelListItemDto
{
    public Guid HotelId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public double Rating { get; set; }
    public decimal PriceFrom { get; set; }
    public string Image { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public bool IsCityMatch { get; set; }
}

public class HotelFullPublicDto
{
    public PublicHotelMetaDto Hotel { get; set; } = new();
    public IReadOnlyCollection<HotelImageDto> Images { get; set; } = Array.Empty<HotelImageDto>();
    public IReadOnlyCollection<FacilityDto> Facilities { get; set; } = Array.Empty<FacilityDto>();
    public IReadOnlyCollection<NearbyPlaceDto> Nearby { get; set; } = Array.Empty<NearbyPlaceDto>();
    public IReadOnlyCollection<HotelRoomTypeDto> RoomTypes { get; set; } = Array.Empty<HotelRoomTypeDto>();
}

public class PublicHotelMetaDto
{
    public Guid HotelId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public string Description { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class HotelImageDto
{
    public string Url { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class FacilityDto
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}

public class NearbyPlaceDto
{
    public string Name { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
}

public class HotelRoomTypeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public int Size { get; set; }
    public string BedType { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string Description { get; set; } = string.Empty;
    public IReadOnlyCollection<string> Facilities { get; set; } = Array.Empty<string>();
    public IReadOnlyCollection<RatePlanDto> RatePlans { get; set; } = Array.Empty<RatePlanDto>();
}

public class RatePlanDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Benefits { get; set; } = string.Empty;
    public string Terms { get; set; } = string.Empty;
}

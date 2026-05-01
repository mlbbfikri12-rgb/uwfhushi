namespace Hotel.Api.DTOs;

public class CityUpsertDto
{
    public string Name { get; set; } = string.Empty;
}

public class CityResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class BrandUpsertDto
{
    public string Name { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
}

public class BrandResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; }
}

public class FacilityUpsertDto
{
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
}

public class FacilityResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public bool IsActive { get; set; }
}

public class HotelUpsertDto
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public Guid CityId { get; set; }
    public Guid? BrandId { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int StarRating { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsActive { get; set; } = true;
}

public class HotelResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public Guid CityId { get; set; }
    public string CityName { get; set; } = string.Empty;
    public Guid? BrandId { get; set; }
    public string? BrandName { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int StarRating { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsActive { get; set; }
    public IReadOnlyCollection<HotelImageAdminDto> Images { get; set; } = Array.Empty<HotelImageAdminDto>();
    public IReadOnlyCollection<HotelFacilityAdminDto> Facilities { get; set; } = Array.Empty<HotelFacilityAdminDto>();
    public IReadOnlyCollection<NearbyPlaceAdminDto> NearbyPlaces { get; set; } = Array.Empty<NearbyPlaceAdminDto>();
}

public class AddHotelImageDto
{
    public string Url { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}

public class HotelImageAdminDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}

public class SetHotelFacilitiesDto
{
    public Guid[] FacilityIds { get; set; } = Array.Empty<Guid>();
}

public class AddNearbyPlaceDto
{
    public string Name { get; set; } = string.Empty;
    public string Distance { get; set; } = string.Empty;
}

public class NearbyPlaceAdminDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Distance { get; set; } = string.Empty;
}

public class HotelFacilityAdminDto
{
    public Guid FacilityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
}

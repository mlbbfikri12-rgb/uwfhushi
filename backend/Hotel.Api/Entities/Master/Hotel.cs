namespace Hotel.Api.Entities.Master;

public class Hotel
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public Guid CityId { get; set; }
    public City? City { get; set; }

    public Guid? BrandId { get; set; }
    public Brand? Brand { get; set; }

    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int StarRating { get; set; } = 3;
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<HotelImage> Images { get; set; } = new List<HotelImage>();
    public ICollection<HotelFacility> HotelFacilities { get; set; } = new List<HotelFacility>();
    public ICollection<NearbyPlace> NearbyPlaces { get; set; } = new List<NearbyPlace>();
}

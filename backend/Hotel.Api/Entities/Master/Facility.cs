namespace Hotel.Api.Entities.Master;

public class Facility
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<HotelFacility> HotelFacilities { get; set; } = new List<HotelFacility>();
}

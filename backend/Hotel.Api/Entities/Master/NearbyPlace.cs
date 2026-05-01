namespace Hotel.Api.Entities.Master;

public class NearbyPlace
{
    public Guid Id { get; set; }
    public Guid HotelId { get; set; }
    public Hotel? Hotel { get; set; }

    public string Name { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public string Distance { get; set; } = string.Empty;
}

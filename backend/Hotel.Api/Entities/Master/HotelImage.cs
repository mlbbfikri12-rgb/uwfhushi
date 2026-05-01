namespace Hotel.Api.Entities.Master;

public class HotelImage
{
    public Guid Id { get; set; }
    public Guid HotelId { get; set; }
    public Hotel? Hotel { get; set; }

    public string Url { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}

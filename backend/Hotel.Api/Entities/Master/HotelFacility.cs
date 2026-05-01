namespace Hotel.Api.Entities.Master;

public class HotelFacility
{
    public Guid HotelId { get; set; }
    public Hotel? Hotel { get; set; }

    public Guid FacilityId { get; set; }
    public Facility? Facility { get; set; }
}

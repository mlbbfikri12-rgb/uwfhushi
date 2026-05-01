namespace Hotel.Api.Entities.Tenant;

public class RoomTypeFacility
{
    public Guid Id { get; set; }
    public Guid RoomTypeId { get; set; }
    public RoomType? RoomType { get; set; }

    public string Name { get; set; } = string.Empty;
}

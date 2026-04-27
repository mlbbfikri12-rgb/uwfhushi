namespace Hotel.Api.Entities.Tenant;

public class RoomType
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public decimal BasePrice { get; set; }

    public int MaxAdults { get; set; }
    public int MaxChildren { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}
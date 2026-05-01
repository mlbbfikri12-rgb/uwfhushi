namespace Hotel.Api.Entities.Tenant;

public class RoomType
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int Size { get; set; }
    public string BedType { get; set; } = string.Empty;
    public int Capacity { get; set; }

    public decimal BasePrice { get; set; }

    public int MaxAdults { get; set; }
    public int MaxChildren { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Room> Rooms { get; set; } = new List<Room>();
    public ICollection<RoomTypeFacility> Facilities { get; set; } = new List<RoomTypeFacility>();
    public ICollection<RatePlan> RatePlans { get; set; } = new List<RatePlan>();
}

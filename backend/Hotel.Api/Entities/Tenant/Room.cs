namespace Hotel.Api.Entities.Tenant;

public class Room
{
    public Guid Id { get; set; }

    public string RoomNumber { get; set; } = string.Empty;

    public Guid RoomTypeId { get; set; }
    public RoomType RoomType { get; set; } = null!;

    public string Status { get; set; } = "available";
    // available | maintenance | occupied

    public string OperationalStatus { get; set; } = RoomOperationalStatuses.Clean;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<RoomAvailability> Availabilities { get; set; } = new List<RoomAvailability>();

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public ICollection<RoomImage> Images { get; set; } = new List<RoomImage>();
}

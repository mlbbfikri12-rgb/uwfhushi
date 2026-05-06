namespace Hotel.Api.Entities.Tenant;

public class RoomAvailability
{
    public Guid Id { get; set; }

    public Guid RoomId { get; set; }

    public Room Room { get; set; } = default!; // 🔥 ADD THIS

    public DateTime Date { get; set; }

    public bool IsAvailable { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
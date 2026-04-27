namespace Hotel.Api.Entities.Tenant;

public class RoomImage
{
    public Guid Id { get; set; }

    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public string Url { get; set; } = string.Empty;
    public string Format { get; set; } = "webp";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
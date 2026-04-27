namespace Hotel.Api.DTOs;

public class RoomResponseDto
{
    public Guid Id { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public RoomTypeResponseDto RoomType { get; set; } = new();
    public IReadOnlyCollection<RoomImageResponseDto> Images { get; set; } = Array.Empty<RoomImageResponseDto>();
}

public class CreateRoomDto
{
    public string RoomNumber { get; set; } = string.Empty;
    public Guid RoomTypeId { get; set; }
    public string Status { get; set; } = "available";
}

public class UpdateRoomDto
{
    public string RoomNumber { get; set; } = string.Empty;
    public Guid RoomTypeId { get; set; }
    public string Status { get; set; } = "available";
}

public class UpdateRoomStatusDto
{
    public string Status { get; set; } = "available";
}

public class RoomImageResponseDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
}

public class AddRoomImageDto
{
    public string Url { get; set; } = string.Empty;
    public string Format { get; set; } = "webp";
}

public class UpdateRoomAvailabilityDto
{
    public DateTime Date { get; set; }
    public bool IsAvailable { get; set; }
}

public class RoomAvailabilityResponseDto
{
    public Guid RoomId { get; set; }
    public DateTime Date { get; set; }
    public bool IsAvailable { get; set; }
}

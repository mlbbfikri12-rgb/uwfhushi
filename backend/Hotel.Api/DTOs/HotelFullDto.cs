namespace Hotel.Api.DTOs;

public class HotelFullDto
{
    public PublicBranchDto Branch { get; set; } = new();
    public IReadOnlyCollection<RoomResponseDto> Rooms { get; set; } = Array.Empty<RoomResponseDto>();
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int AdultCount { get; set; }
    public int ChildCount { get; set; }
}

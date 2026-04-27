namespace Hotel.Api.DTOs;

public class RoomTypeResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public int MaxAdults { get; set; }
    public int MaxChildren { get; set; }
}

public class CreateRoomTypeDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public int MaxAdults { get; set; }
    public int MaxChildren { get; set; }
}

public class UpdateRoomTypeDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public int MaxAdults { get; set; }
    public int MaxChildren { get; set; }
}

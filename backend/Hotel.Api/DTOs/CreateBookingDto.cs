namespace Hotel.Api.DTOs;

public class CreateBookingDto
{
    public Guid RoomId { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;

    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }

    public int AdultCount { get; set; }
    public int ChildCount { get; set; }

    public string? PaymentMethod { get; set; }
    public string? Notes { get; set; }
}

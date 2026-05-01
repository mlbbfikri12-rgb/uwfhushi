namespace Hotel.Api.DTOs;

public class AddOrderItemDto
{
    public Guid RoomTypeId { get; set; }
    public Guid RatePlanId { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int TotalRooms { get; set; } = 1;
}

public class OrderCurrentDto
{
    public Guid OrderDraftId { get; set; }
    public IReadOnlyCollection<OrderItemDto> Items { get; set; } = Array.Empty<OrderItemDto>();
    public decimal GrandTotal { get; set; }
}

public class OrderItemDto
{
    public Guid Id { get; set; }
    public Guid RoomTypeId { get; set; }
    public Guid RatePlanId { get; set; }
    public string RoomTypeName { get; set; } = string.Empty;
    public string RatePlanName { get; set; } = string.Empty;
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int TotalRooms { get; set; }
    public decimal PricePerNight { get; set; }
    public decimal TotalPrice { get; set; }
}

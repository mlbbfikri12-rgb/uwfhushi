namespace Hotel.Api.Entities.Tenant;

public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderDraftId { get; set; }
    public OrderDraft? OrderDraft { get; set; }

    public Guid RoomTypeId { get; set; }
    public RoomType? RoomType { get; set; }

    public Guid RatePlanId { get; set; }
    public RatePlan? RatePlan { get; set; }

    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int TotalRooms { get; set; }
    public decimal PricePerNight { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

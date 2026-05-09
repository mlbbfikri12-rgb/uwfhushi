namespace Hotel.Api.Entities.Tenant;

public class BookingGroup
{
    public Guid Id { get; set; }
    public string GroupCode { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public string Status { get; set; } = "pending";
    public decimal TotalAmount { get; set; }
    public DateTime HoldUntilUtc { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}

namespace Hotel.Api.Entities.Tenant;

public class Booking
{
    public Guid Id { get; set; }

    // 🔥 RELATION
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;

    // 🔥 DATE
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }

    // 🔥 GUEST INFO
    public int AdultCount { get; set; }
    public int ChildCount { get; set; }

    // 🔥 PRICING
    public decimal BasePrice { get; set; }
    public decimal Tax { get; set; }
    public decimal TotalPrice { get; set; }

    // 🔥 STATUS
    public string Status { get; set; } = "pending";
    // pending | paid | cancelled | completed

    // 🔥 PAYMENT
    public string? PaymentMethod { get; set; }
    public string? PaymentStatus { get; set; }
    public DateTime? PaidAt { get; set; }

    // 🔥 BOOKING CODE (penting banget)
    public string BookingCode { get; set; } = string.Empty;

    // 🔥 META
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
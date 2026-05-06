namespace Hotel.Api.Entities.Tenant;

public class Payment
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public decimal Amount { get; set; }

    public string Method { get; set; } = string.Empty;
    // midtrans, cash, transfer

    public string Status { get; set; } = "pending";
    // pending | paid | failed

    public string? TransactionId { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
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

public class CheckoutOrderDto
{
    public int AdultCount { get; set; } = 2;
    public int ChildCount { get; set; } = 0;
    public string? PaymentMethod { get; set; }
    public string? Notes { get; set; }
}

public class CheckoutOrderResponseDto
{
    public string Message { get; set; } = "Order checkout success";
    public Guid OrderDraftId { get; set; }
    public decimal GrandTotal { get; set; }
    public IReadOnlyCollection<CheckoutBookingItemDto> Bookings { get; set; } = Array.Empty<CheckoutBookingItemDto>();
}

public class CheckoutBookingItemDto
{
    public Guid BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public Guid RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public string RatePlanName { get; set; } = string.Empty;
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public decimal TotalPrice { get; set; }
}

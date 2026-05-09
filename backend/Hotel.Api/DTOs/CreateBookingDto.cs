namespace Hotel.Api.DTOs;

public class CreateBookingDto
{
    public Guid RoomTypeId { get; set; }
    public Guid RatePlanId { get; set; }

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
    public string BookingGroupCode { get; set; } = string.Empty;
    public Guid OrderDraftId { get; set; }
    public decimal GrandTotal { get; set; }
    public IReadOnlyCollection<CheckoutBookingItemDto> Bookings { get; set; } = Array.Empty<CheckoutBookingItemDto>();
}

public class GuestCheckoutDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public int AdultCount { get; set; } = 2;
    public int ChildCount { get; set; } = 0;
    public string? PaymentMethod { get; set; }
    public string? Notes { get; set; }
    public IReadOnlyCollection<GuestCheckoutItemDto> Items { get; set; } = Array.Empty<GuestCheckoutItemDto>();
}

public class GuestCheckoutItemDto
{
    public Guid RoomTypeId { get; set; }
    public Guid RatePlanId { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int TotalRooms { get; set; } = 1;
}

public class GuestCheckoutResponseDto
{
    public string Message { get; set; } = "Guest checkout success";
    public string BookingGroupCode { get; set; } = string.Empty;
    public decimal GrandTotal { get; set; }
    public IReadOnlyCollection<CheckoutBookingItemDto> Bookings { get; set; } = Array.Empty<CheckoutBookingItemDto>();
}

public class CheckoutBookingItemDto
{
    public Guid BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public Guid? RoomId { get; set; }
    public string? RoomNumber { get; set; }
    public Guid RoomTypeId { get; set; }
    public string RoomTypeName { get; set; } = string.Empty;
    public string RatePlanName { get; set; } = string.Empty;
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public decimal TotalPrice { get; set; }
}

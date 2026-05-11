namespace Hotel.Api.DTOs;

public class AdminPagedResultDto<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);
    public IReadOnlyCollection<T> Items { get; set; } = Array.Empty<T>();
}

public class AdminBookingGroupQueryDto
{
    public string? Q { get; set; }
    public string? Status { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class AdminBookingGroupListItemDto
{
    public Guid Id { get; set; }
    public string GroupCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public int BookingCount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime HoldUntilUtc { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminBookingGroupDetailDto
{
    public Guid Id { get; set; }
    public string GroupCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime HoldUntilUtc { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public AdminBookingCustomerDto Customer { get; set; } = new();
    public IReadOnlyCollection<AdminBookingItemDto> Bookings { get; set; } = Array.Empty<AdminBookingItemDto>();
    public IReadOnlyCollection<AdminPaymentEventDto> PaymentEvents { get; set; } = Array.Empty<AdminPaymentEventDto>();
}

public class AdminBookingCustomerDto
{
    public Guid Id { get; set; }
    public Guid GlobalCustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

public class AdminBookingItemDto
{
    public Guid Id { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public Guid RoomTypeId { get; set; }
    public string RoomTypeName { get; set; } = string.Empty;
    public Guid? RoomId { get; set; }
    public string? RoomNumber { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int AdultCount { get; set; }
    public int ChildCount { get; set; }
    public decimal BasePrice { get; set; }
    public decimal Tax { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? ConfirmedAtUtc { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminPaymentEventQueryDto
{
    public string? Q { get; set; }
    public string? Status { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class AdminPaymentEventDto
{
    public Guid Id { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string PaymentType { get; set; } = string.Empty;
    public string TransactionStatus { get; set; } = string.Empty;
    public string MappedStatus { get; set; } = string.Empty;
    public decimal GrossAmount { get; set; }
    public string ProcessingStatus { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}

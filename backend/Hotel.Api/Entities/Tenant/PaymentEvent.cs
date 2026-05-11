namespace Hotel.Api.Entities.Tenant;

public class PaymentEvent
{
    public Guid Id { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string PaymentType { get; set; } = string.Empty;
    public string TransactionStatus { get; set; } = string.Empty;
    public string MappedStatus { get; set; } = string.Empty;
    public decimal GrossAmount { get; set; }
    public string ProcessingStatus { get; set; } = "received";
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

using System.Text.Json.Serialization;

namespace Hotel.Api.DTOs;

public class MidtransWebhookDto
{
    [JsonPropertyName("order_id")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("transaction_id")]
    public string TransactionId { get; set; } = string.Empty;

    [JsonPropertyName("transaction_status")]
    public string TransactionStatus { get; set; } = string.Empty;

    [JsonPropertyName("payment_type")]
    public string PaymentType { get; set; } = string.Empty;

    [JsonPropertyName("gross_amount")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal GrossAmount { get; set; }
}

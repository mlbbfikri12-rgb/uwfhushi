namespace Hotel.Api.Entities.Tenant;

public class RatePlan
{
    public Guid Id { get; set; }
    public Guid RoomTypeId { get; set; }
    public RoomType? RoomType { get; set; }

    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IncludesBreakfast { get; set; }
    public bool IsRefundable { get; set; }
    public string PaymentType { get; set; } = "online";
    public string TermsConditions { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

namespace Hotel.Api.DTOs;

public class PublicSearchResultDto
{
    public string Type { get; set; } = string.Empty; // "city" | "hotel"
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public double Score { get; set; }
}

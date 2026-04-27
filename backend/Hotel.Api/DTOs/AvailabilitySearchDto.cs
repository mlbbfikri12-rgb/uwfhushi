namespace Hotel.Api.DTOs;

public class AvailabilitySearchDto
{
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int AdultCount { get; set; }
    public int ChildCount { get; set; }
}

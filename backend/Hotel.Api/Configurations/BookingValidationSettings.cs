namespace Hotel.Api.Configurations;

public class BookingValidationSettings
{
    public int MaxStayNights { get; set; } = 30;
    public int MaxAdvanceBookingDays { get; set; } = 365;
}

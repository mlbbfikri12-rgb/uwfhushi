namespace Hotel.Api.Configurations;

public class BookingValidationSettings
{
    public int MaxStayNights { get; set; } = 30;
    public int MaxAdvanceBookingDays { get; set; } = 365;
    public int PendingHoldMinutes { get; set; } = 15;
    public int PendingExpirySweepMinutes { get; set; } = 1;
}

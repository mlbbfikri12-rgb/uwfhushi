namespace Hotel.Api.Configurations;

public class RateLimitSettings
{
    public int GlobalPermitLimit { get; set; } = 100;
    public int GlobalWindowSeconds { get; set; } = 60;
    public int BookingPermitLimit { get; set; } = 5;
    public int BookingWindowSeconds { get; set; } = 60;
    public int AuthLoginPermitLimit { get; set; } = 5;
    public int AuthRegisterPermitLimit { get; set; } = 3;
    public int AuthWindowSeconds { get; set; } = 60;
    public bool UseRedisForBookingLimit { get; set; } = true;
}

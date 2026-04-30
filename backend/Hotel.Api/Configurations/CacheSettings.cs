namespace Hotel.Api.Configurations;

public class CacheSettings
{
    public string RedisConnection { get; set; } = "localhost:6379";
    public int DefaultTtlMinutes { get; set; } = 5;
    public int HotelFullTtlMinutes { get; set; } = 10;
    public int BranchSearchTtlMinutes { get; set; } = 5;
    public int AvailabilityTtlMinutes { get; set; } = 5;
}

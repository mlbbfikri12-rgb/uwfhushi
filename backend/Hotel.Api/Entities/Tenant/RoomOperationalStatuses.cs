namespace Hotel.Api.Entities.Tenant;

public static class RoomOperationalStatuses
{
    public const string Clean = "clean";
    public const string Dirty = "dirty";
    public const string Occupied = "occupied";
    public const string Cleaning = "cleaning";
    public const string Maintenance = "maintenance";
    public const string OutOfOrder = "out_of_order";

    public static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        Clean,
        Dirty,
        Occupied,
        Cleaning,
        Maintenance,
        OutOfOrder
    };
}

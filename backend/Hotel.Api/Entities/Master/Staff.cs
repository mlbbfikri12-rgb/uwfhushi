namespace Hotel.Api.Entities.Master;

public class Staff
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = StaffRoles.FO;
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<StaffBranch> StaffBranches { get; set; } = new List<StaffBranch>();
}

public static class StaffRoles
{
    public const string SuperAdmin = "SUPER_ADMIN";
    public const string SPV = "SPV";
    public const string FO = "FO";
}

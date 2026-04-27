namespace Hotel.Api.Entities.Master;

public class Branch
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    public string DbName { get; set; } = string.Empty;
    public string DbHost { get; set; } = "localhost";
    public int DbPort { get; set; } = 5432;

    public string DbUser { get; set; } = "postgres";
    public string DbPassword { get; set; } = "postgres";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<StaffBranch> StaffBranches { get; set; } = new List<StaffBranch>();
}

namespace Hotel.Api.Entities.Master;

public class StaffBranch
{
    public Guid Id { get; set; }

    public Guid StaffId { get; set; }
    public Staff Staff { get; set; } = null!;

    public Guid BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

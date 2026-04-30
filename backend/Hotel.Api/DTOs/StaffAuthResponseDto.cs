namespace Hotel.Api.DTOs;

public class StaffAuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public Guid StaffId { get; set; }
    public string Role { get; set; } = string.Empty;
    public IReadOnlyCollection<Guid> AllowedBranchIds { get; set; } = Array.Empty<Guid>();
    public IReadOnlyCollection<StaffAllowedBranchDto> AllowedBranches { get; set; } = Array.Empty<StaffAllowedBranchDto>();
}

public class StaffAllowedBranchDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

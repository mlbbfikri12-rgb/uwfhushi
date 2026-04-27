namespace Hotel.Api.DTOs;

public class StaffAuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public Guid StaffId { get; set; }
    public string Role { get; set; } = string.Empty;
    public IReadOnlyCollection<Guid> AllowedBranchIds { get; set; } = Array.Empty<Guid>();
}

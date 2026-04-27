namespace Hotel.Api.DTOs;

public class BranchResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string DbName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

namespace Hotel.Api.DTOs;

public class CreateBranchDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    public string? DbHost { get; set; }
    public int? DbPort { get; set; }
    public string? DbUser { get; set; }
    public string? DbPassword { get; set; }
}

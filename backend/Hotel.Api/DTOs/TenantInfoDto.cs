namespace Hotel.Api.DTOs;

public class TenantInfo
{
    public string ConnectionString { get; set; } = default!;
    public Guid BranchId { get; set; }
}

public class TenantCacheDto
{
    public Guid BranchId { get; set; }
    public string ConnectionString { get; set; } = default!;
}